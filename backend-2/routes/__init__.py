# routes/__init__.py
from .geocode import geocode_bp
from .weather import weather_bp
from .solar import solar_bp


def register_blueprints(app):
    app.register_blueprint(geocode_bp)
    app.register_blueprint(weather_bp)
    app.register_blueprint(solar_bp)
