## Language manager with Geo location CS2 Plugin
Language managin Plugin for CounterStrikeSharp. Uses a Geo Country DB to match ip addresses
to a specific country. Should translate all Plugins within CounterStrikeSharp. Also saves
users specified language.

> [!WARNING]  
> Only works with ISO codes e.g.: `!lang en` or `!lang lv`

On first connect checks if player has been saved in the database.
If has then uses the saved playerISOCode for lang.
If not gets the language based on ipAddress from GeoLocation database
Saves the value to database

Requires `GeoLite2-Country.mmdb` to be inside the Plugin root folder.
You can get it from: https://dev.maxmind.com/geoip/geolite2-free-geolocation-data
Or from release

Idea by: @rcon420
