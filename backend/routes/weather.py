# routes/weather.py
from flask import Blueprint, request, jsonify
import requests

weather_bp = Blueprint('weather', __name__, url_prefix='/api/weather')


@weather_bp.route('/', methods=['POST'])
def weather():
    """GPS -> teplota a vietor"""
    data = request.get_json() or {}
    lat = data.get('lat')
    lon = data.get('lon')

    if lat is None or lon is None:
        return jsonify({'error': 'lat and lon are required'}), 400

    response = requests.get(
        'https://api.open-meteo.com/v1/forecast',
        params={
            'latitude': lat,
            'longitude': lon,
            'current': 'temperature_2m,windspeed_10m',
            'daily': 'temperature_2m_mean,windspeed_10m_mean',
            'forecast_days': 7
        }
    )

    data = response.json()
    daily = data.get('daily', {})

    temps = daily.get('temperature_2m_mean', [])
    winds = daily.get('windspeed_10m_mean', [])

    avg_temp = sum(temps) / len(temps) if temps else 0
    avg_wind = sum(winds) / len(winds) if winds else 0

    return jsonify({
        'current_temp': data.get('current', {}).get('temperature_2m'),
        'current_wind': data.get('current', {}).get('windspeed_10m'),
        'avg_temp': avg_temp,
        'avg_wind': avg_wind,
        'wind_suitable': avg_wind > 4.5
    })
