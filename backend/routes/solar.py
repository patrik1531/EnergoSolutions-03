# routes/solar.py
from flask import Blueprint, request, jsonify
import requests

solar_bp = Blueprint('solar', __name__, url_prefix='/api/solar')


@solar_bp.route('/', methods=['POST'])
def solar_resource():
    """
    GPS -> solárny potenciál z PVGIS pre 1 kWp na rôzne svetové strany
    pri jednom optimálnom sklone pre danú lokalitu.

    Vstup JSON:
    {
      "lat": float,                  # povinné
      "lon": float,                  # povinné
      "system_loss_percent": 14      # voliteľné, default 14 %
    }

    Výstup JSON (príklad):
    {
      "location": {...},
      "system_config": {
        "peak_power_kw": 1.0,
        "system_loss_percent": 14.0,
        "optimal_tilt_deg": 34.5
      },
      "solar_resource": {
        "orientations": [
          {
            "orientation": "south",
            "aspect_deg": 0.0,
            "kwh_per_kwp_year": 1180.0,
            "relative_to_south": 1.0
          },
          {
            "orientation": "east",
            "aspect_deg": -90.0,
            "kwh_per_kwp_year": 1040.0,
            "relative_to_south": 0.88
          },
          ...
        ],
        "best_orientation": "south"
      }
    }
    """
    payload = request.get_json() or {}
    lat = payload.get("lat")
    lon = payload.get("lon")
    system_loss_percent = float(payload.get("system_loss_percent", 14.0))

    if lat is None or lon is None:
        return jsonify({"error": "lat and lon are required"}), 400

    peak_power_kw = 1.0  # hodnotíme potenciál na 1 kWp

    # 1) Najprv z PVGIS zistíme optimálny sklon pre danú lokalitu
    base_params = {
        "lat": lat,
        "lon": lon,
        "peakpower": peak_power_kw,
        "loss": system_loss_percent,
        "outputformat": "json",
        "mountingplace": "building",
    }

    try:
        # optimalangles=1 -> PVGIS vyberie optimálny sklon a orientáciu
        optimal_resp = requests.get(
            "https://re.jrc.ec.europa.eu/api/v5_2/PVcalc",
            params={**base_params, "optimalangles": 1},
            timeout=20,
        )
        optimal_resp.raise_for_status()
    except requests.RequestException as e:
        return jsonify({"error": "Failed to fetch optimal tilt from PVGIS", "details": str(e)}), 502

    optimal_payload = optimal_resp.json()
    fixed_inputs = (
        optimal_payload
        .get("outputs", {})
        .get("inputs", {})
        .get("mounting_system", {})
        .get("fixed", {})
        or {}
    )

    optimal_tilt = float(fixed_inputs.get("angle", 35.0))  # fallback napr. 35°

    # 2) Pre každú svetovú stranu zavoláme PVGIS s týmto sklonom a rôznym azimutom
    # PVGIS aspekt: 0 = juh, -90 = východ, 90 = západ, 180 = sever
    orientations = [
        ("south", 0.0),
        ("east", -90.0),
        ("west", 90.0),
        ("north", 180.0),
    ]

    results = []
    errors = []

    for name, aspect in orientations:
        params = {
            **base_params,
            "optimalangles": 0,       # už špecifikujeme vlastný uhol
            "angle": optimal_tilt,
            "aspect": aspect,
        }

        try:
            resp = requests.get(
                "https://re.jrc.ec.europa.eu/api/v5_2/PVcalc",
                params=params,
                timeout=20,
            )
            resp.raise_for_status()
        except requests.RequestException as e:
            errors.append(f"{name}: {str(e)}")
            continue

        payload = resp.json()
        totals_fixed = (
            payload
            .get("outputs", {})
            .get("totals", {})
            .get("fixed", {})
            or {}
        )

        # E_y = ročný výnos (kWh) pri zadanom peakpower
        yearly_kwh = float(totals_fixed.get("E_y", 0.0))

        results.append({
            "orientation": name,
            "aspect_deg": aspect,
            "kwh_per_kwp_year": yearly_kwh,  # lebo peakpower = 1 kWp
        })

    if not results:
        return jsonify({"error": "Failed to fetch solar resource for all orientations", "details": errors}), 502

    # 3) Relatívne faktory voči juhu a best_orientation
    south_value = next((r["kwh_per_kwp_year"] for r in results if r["orientation"] == "south"), None)

    best_orientation = max(results, key=lambda r: r["kwh_per_kwp_year"])["orientation"]

    for r in results:
        if south_value and south_value > 0:
            r["relative_to_south"] = r["kwh_per_kwp_year"] / south_value
        else:
            r["relative_to_south"] = None

    response_body = {
        "location": {"lat": lat, "lon": lon},
        "system_config": {
            "peak_power_kw": peak_power_kw,
            "system_loss_percent": system_loss_percent,
            "optimal_tilt_deg": optimal_tilt,
        },
        "solar_resource": {
            "orientations": results,
            "best_orientation": best_orientation,
        },
    }

    if errors:
        response_body["warnings"] = errors

    return jsonify(response_body)
