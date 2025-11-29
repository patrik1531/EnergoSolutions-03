# routes/wind.py
from flask import Blueprint, request, jsonify
import requests
from datetime import datetime, date

wind_bp = Blueprint('wind', __name__, url_prefix='/api/wind')


@wind_bp.route('/', methods=['POST'])
def climate_wind():
    """
    GPS -> climate_wind štatistiky za ~5 rokov:
    - rozdelenie rýchlosti vetra do intervalov (hodiny/rok + %)
    - priemerná rýchlosť vetra za rok
    - počet hodín nad 3 m/s a nad 6 m/s
    """
    data = request.get_json() or {}
    lat = data.get('lat')
    lon = data.get('lon')

    if lat is None or lon is None:
        return jsonify({'error': 'lat and lon are required'}), 400

    # ---- 1) Obdobie: posledných ~5 rokov ----
    today = date.today()
    start_date = date(today.year - 5, 1, 1)
    end_date = today

    # ---- 2) Stiahneme historické hodinové rýchlosti vetra z Open-Meteo ----
    try:
        response = requests.get(
            'https://archive-api.open-meteo.com/v1/archive',
            params={
                'latitude': lat,
                'longitude': lon,
                'start_date': start_date.isoformat(),
                'end_date': end_date.isoformat(),
                'hourly': 'windspeed_10m',
                'timezone': 'auto',
            },
            timeout=20
        )
        response.raise_for_status()
    except requests.RequestException as e:
        return jsonify({'error': 'Failed to fetch wind data', 'details': str(e)}), 502

    payload = response.json()
    hourly = payload.get('hourly', {})
    times = hourly.get('time', [])
    speeds = hourly.get('windspeed_10m', [])

    if not times or not speeds or len(times) != len(speeds):
        return jsonify({'error': 'Invalid wind data from provider'}), 502

    # ---- 3) Definujeme intervaly rýchlosti vetra ----
    # (label, lower_inclusive, upper_exclusive) - None = otvorený koniec
    wind_bins_def = [
        ("<= 3 m/s", None, 3.0),
        ("3 - 6 m/s", 3.0, 6.0),
        ("6 - 9 m/s", 6.0, 9.0),
        ("9 - 12 m/s", 9.0, 12.0),
        ("> 12 m/s", 12.0, None),
    ]

    def classify_wind(v: float) -> str:
        for label, lo, hi in wind_bins_def:
            if lo is None and v <= hi:
                return label
            if hi is None and v > lo:
                return label
            if lo is not None and hi is not None and lo <= v < hi:
                return label
        return "unknown"

    # per-year štatistiky
    years_stats = {}

    # ---- 4) Prejdeme všetky hodiny a plníme štatistiky ----
    for t_str, speed in zip(times, speeds):
        # Open-Meteo formát: "YYYY-MM-DDTHH:MM"
        dt = datetime.fromisoformat(t_str)
        year = dt.year

        if year not in years_stats:
            years_stats[year] = {
                "bin_counts": {label: 0 for (label, _, _) in wind_bins_def},
                "total_hours": 0,
                "sum_speed": 0.0,
                "hours_above_3": 0,
                "hours_above_6": 0,
            }

        ys = years_stats[year]

        # binovanie
        label = classify_wind(speed)
        if label in ys["bin_counts"]:
            ys["bin_counts"][label] += 1

        ys["total_hours"] += 1
        ys["sum_speed"] += speed

        if speed > 3.0:
            ys["hours_above_3"] += 1
        if speed > 6.0:
            ys["hours_above_6"] += 1

    # ---- 5) Prepočty na percentá + multi-year priemer ----
    multi_year_bin_hours = {label: 0 for (label, _, _) in wind_bins_def}
    multi_year_total_hours = 0
    multi_year_sum_speed = 0.0

    years_output = []

    for year in sorted(years_stats.keys()):
        ys = years_stats[year]
        total_hours = ys["total_hours"] or 1  # ochrana pred /0

        bins_output = []
        for label, _, _ in wind_bins_def:
            hours = ys["bin_counts"][label]
            percent_of_year = (hours / total_hours) * 100.0
            bins_output.append({
                "range": label,
                "hours": hours,
                "percent_of_year": percent_of_year,
            })
            multi_year_bin_hours[label] += hours

        mean_speed = ys["sum_speed"] / total_hours

        multi_year_total_hours += total_hours
        multi_year_sum_speed += ys["sum_speed"]

        years_output.append({
            "year": year,
            "wind_bins": bins_output,
            "mean_speed": mean_speed,
            "hours_above_3": ys["hours_above_3"],
            "hours_above_6": ys["hours_above_6"],
            "total_hours": ys["total_hours"],
        })

    multi_year_bins_output = []
    overall_mean_speed = None

    if multi_year_total_hours > 0:
        for label, _, _ in wind_bins_def:
            hours = multi_year_bin_hours[label]
            percent = (hours / multi_year_total_hours) * 100.0
            multi_year_bins_output.append({
                "range": label,
                "avg_percent_of_year": percent
            })
        overall_mean_speed = multi_year_sum_speed / multi_year_total_hours

    climate_wind = {
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
            "wind_bins_avg_percent": multi_year_bins_output,
            "overall_mean_speed": overall_mean_speed,
            "total_years": len(years_stats),
        }
    }

    return jsonify(climate_wind)
