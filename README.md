## Language manager with Geo location

On first connect checks if player has been saved in the database.
If has then uses the saved playerISOCode for lang.
If not gets the language based on ipAddress from GeoLocation database
Saves the value to database

Requires `GeoLite2-Country.mmdb` to be inside the Plugin root folder.
You can get it from: https://dev.maxmind.com/geoip/geolite2-free-geolocation-data
Or from release

Idea by: @rcon420
