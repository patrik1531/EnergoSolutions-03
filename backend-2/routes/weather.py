# routes/weather.py
from flask import Blueprint, request, jsonify
import requests
from datetime import datetime, date

weather_bp = Blueprint('weather', __name__, url_prefix='/api/weather')


@weather_bp.route('/', methods=['POST'])
def climate_heating():
    """
    GPS -> climate_heating štatistiky za ~5 rokov:
    - rozdelenie teplôt do intervalov (hodiny/rok + %)
    - HDD(20 °C) pre každý rok
    - extrémy (min. teplota, hodiny pod -10 / -15 °C)
    """
    data = request.get_json() or {}
    lat = data.get('lat')
    lon = data.get('lon')

    if lat is None or lon is None:
        return jsonify({'error': 'lat and lon are required'}), 400

    # ---- 1) Určíme obdobie: posledných ~5 rokov ----
    today = date.today()
    start_date = date(today.year - 5, 1, 1)   # od 1.1. pred 5 rokmi
    end_date = today                          # do dnes

    # ---- 2) Stiahneme historické hodinové teploty z Open-Meteo ----
    try:
        response = requests.get(
            'https://archive-api.open-meteo.com/v1/archive',
            params={
                'latitude': lat,
                'longitude': lon,
                'start_date': start_date.isoformat(),
                'end_date': end_date.isoformat(),
                'hourly': 'temperature_2m',
                'timezone': 'auto',
            },
            timeout=20
        )
        response.raise_for_status()
    except requests.RequestException as e:
        return jsonify({'error': 'Failed to fetch weather data', 'details': str(e)}), 502

    payload = response.json()
    hourly = payload.get('hourly', {})
    times = hourly.get('time', [])
    temps = hourly.get('temperature_2m', [])

    if not times or not temps or len(times) != len(temps):
        return jsonify({'error': 'Invalid weather data from provider'}), 502

    # ---- 3) Pripravíme štruktúry na štatistiky ----

    # definícia teplotných intervalov
    # (label, lower_inclusive, upper_exclusive) - None = otvorený koniec
    temp_bins_def = [
        ("<= -15", None, -15.0),
        ("-15 až -10", -15.0, -10.0),
        ("-10 až -5", -10.0, -5.0),
        ("-5 až 0", -5.0, 0.0),
        ("0 až +5", 0.0, 5.0),
        ("+5 až +10", 5.0, 10.0),
        ("+10 až +15", 10.0, 15.0),
        ("> +15", 15.0, None),
    ]

    def classify_temp(t: float) -> str:
        for label, lo, hi in temp_bins_def:
            if lo is None and t <= hi:
                return label
            if hi is None and t > lo:
                return label
            if lo is not None and hi is not None and lo <= t < hi:
                return label
        return "unknown"

    # per-year štatistiky
    years_stats = {}

    # pomocné: HDD počítame z denných priemerov
    daily_temps = {}  # { date(): [temps] }

    # ---- 4) Prejdeme všetky hodiny a plníme štatistiky ----
    for t_str, temp in zip(times, temps):
        # Open-Meteo formát: "YYYY-MM-DDTHH:MM"
        dt = datetime.fromisoformat(t_str)
        year = dt.year
        d = dt.date()

        # inicializácia pre rok
        if year not in years_stats:
            years_stats[year] = {
                "bin_counts": {label: 0 for (label, _, _) in temp_bins_def},
                "total_hours": 0,
                "min_temp": None,
                "hours_below_minus10": 0,
                "hours_below_minus15": 0,
                "hdd_20": 0.0,  # dopočítame neskôr z daily priemerov
            }

        ys = years_stats[year]

        # teplotný interval
        label = classify_temp(temp)
        if label in ys["bin_counts"]:
            ys["bin_counts"][label] += 1

        ys["total_hours"] += 1

        # extrémy
        if ys["min_temp"] is None or temp < ys["min_temp"]:
            ys["min_temp"] = temp

        if temp < -10.0:
            ys["hours_below_minus10"] += 1
        if temp < -15.0:
            ys["hours_below_minus15"] += 1

        # denný priemer -> HDD
        if d not in daily_temps:
            daily_temps[d] = []
        daily_temps[d].append(temp)

    # ---- 5) HDD(20 °C) z denných priemerov ----
    for d, t_list in daily_temps.items():
        avg_day_temp = sum(t_list) / len(t_list)
        if avg_day_temp < 20.0:
            hdd = 20.0 - avg_day_temp
            year = d.year
            if year in years_stats:
                years_stats[year]["hdd_20"] += hdd

    # ---- 6) Prepočty na percentá + multi-year priemer ----
    # multi-year agregácia bin percent
    multi_year_bin_hours = {label: 0 for (label, _, _) in temp_bins_def}
    multi_year_total_hours = 0

    years_output = []

    for year in sorted(years_stats.keys()):
        ys = years_stats[year]
        total_hours = ys["total_hours"] or 1  # ochrana pred delením nulou

        # prepočty na percentá z roka
        bins_output = []
        for label, _, _ in temp_bins_def:
            hours = ys["bin_counts"][label]
            percent_of_year = (hours / total_hours) * 100.0
            bins_output.append({
                "range": label,
                "hours": hours,
                "percent_of_year": percent_of_year,
            })

            multi_year_bin_hours[label] += hours

        multi_year_total_hours += total_hours

        years_output.append({
            "year": year,
            "temp_bins": bins_output,
            "hdd_20": ys["hdd_20"],
            "min_temp": ys["min_temp"],
            "hours_below_minus10": ys["hours_below_minus10"],
            "hours_below_minus15": ys["hours_below_minus15"],
            "total_hours": ys["total_hours"],
        })

    # multi-year priemer percent
    multi_year_bins_output = []
    if multi_year_total_hours > 0:
        for label, _, _ in temp_bins_def:
            hours = multi_year_bin_hours[label]
            percent = (hours / multi_year_total_hours) * 100.0
            multi_year_bins_output.append({
                "range": label,
                "avg_percent_of_year": percent
            })

    climate_heating = {
        "location": {
            "lat": lat,
            "lon": lon,
        },
        "period": {
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat(),
        },
        "years": years_output,
        "multi_year": {
            "temp_bins_avg_percent": multi_year_bins_output,
            "total_years": len(years_stats),
        }
    }

    return jsonify(climate_heating)
