# routes/geocode.py
from flask import Blueprint, request, jsonify
import requests

geocode_bp = Blueprint('geocode', __name__, url_prefix='/api/geocode')


@geocode_bp.route('/', methods=['POST'])
def geocode():
    address = request.json.get('address')

    response = requests.get(
        'https://nominatim.openstreetmap.org/search',
        params={'q': address, 'format': 'json', 'limit': 1},
        headers={'User-Agent': 'GreenEnergyApp/1.0'}
    )

    data = response.json()
    if not data:
        return jsonify({'error': 'Address not found'}), 404

    return jsonify({
        'lat': float(data[0]['lat']),
        'lon': float(data[0]['lon']),
        'name': data[0].get('display_name', '')
    })