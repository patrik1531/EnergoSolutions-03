# routes/solar.py
from flask import Blueprint, request, jsonify
import requests

solar_bp = Blueprint('solar', __name__, url_prefix='/api/solar')


@solar_bp.route('/', methods=['POST'])
def solar():
    """GPS -> solárny potenciál"""
    data = request.get_json() or {}
    lat = data.get('lat')
    lon = data.get('lon')

    if lat is None or lon is None:
        return jsonify({'error': 'lat and lon are required'}), 400

    response = requests.get(
        'https://re.jrc.ec.europa.eu/api/v5_2/PVcalc',
        params={
            'lat': lat,
            'lon': lon,
            'peakpower': 1,
            'loss': 14,
            'outputformat': 'json',
            'optimalangles': 1
        }
    )

    data = response.json()
    outputs = data.get('outputs', {})
    totals = outputs.get('totals', {}).get('fixed', {})

    return jsonify({
        'yearly_kwh_per_kw': totals.get('E_y', 0),
        'daily_avg': totals.get('E_d', 0),
        'optimal_angle': outputs.get('inputs', {}).get('mounting_system', {}).get('fixed', {}).get('angle', 35),
        'solar_suitable': totals.get('E_y', 0) > 900
    })
