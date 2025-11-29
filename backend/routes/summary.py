# routes/summary.py
from flask import Blueprint, request, jsonify
import requests

summary_bp = Blueprint('summary', __name__, url_prefix='/api/summary')


@summary_bp.route('/', methods=['POST'])
def climate_summary():
    """
    Jeden super-endpoint:
    - zavolá heating
    - zavolá wind
    - zavolá solar/resource
    - spojí výsledky do jedného JSON pre AI
    """

    payload = request.get_json() or {}
    lat = payload.get("lat")
    lon = payload.get("lon")

    if lat is None or lon is None:
        return jsonify({"error": "lat and lon are required"}), 400

    BASE = "http://127.0.0.1:5000"

    errors = []

    # ----------- HEATING -----------
    try:
        heating_resp = requests.post(
            f"{BASE}/api/weather/",
            json={"lat": lat, "lon": lon},
            timeout=20
        )
        heating_resp.raise_for_status()
        heating_data = heating_resp.json()
    except Exception as e:
        heating_data = None
        errors.append({"heating": str(e)})

    # ----------- WIND -----------
    try:
        wind_resp = requests.post(
            f"{BASE}/api/wind/",
            json={"lat": lat, "lon": lon},
            timeout=30
        )
        wind_resp.raise_for_status()
        wind_data = wind_resp.json()
    except Exception as e:
        wind_data = None
        errors.append({"wind": str(e)})

    # ----------- SOLAR -----------
    try:
        solar_resp = requests.post(
            f"{BASE}/api/solar",
            json={"lat": lat, "lon": lon},
            timeout=30
        )
        solar_resp.raise_for_status()
        solar_data = solar_resp.json()
    except Exception as e:
        solar_data = None
        errors.append({"solar": str(e)})

    # ----------- Výsledný JSON -----------
    summary = {
        "location": {"lat": lat, "lon": lon},
        "climate_heating": heating_data,
        "climate_wind": wind_data,
        "solar_resource": solar_data,
    }

    if errors:
        summary["warnings"] = errors

    return jsonify(summary)
