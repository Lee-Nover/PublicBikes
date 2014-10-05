exports.serviceName = "hourbike";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    switch (cityName) {
        case 'liverpool':
            return 'http://www.citybikeliverpool.co.uk/LocationsMap.aspx';
        case 'szczecin':
            return 'https://www.bikes-srm.pl/LocationsMap.aspx';
        default:
            return '';
    }
}

exports.extractData = function (data, cityName) {
    switch (cityName) {
        case 'liverpool':
        case 'szczecin':
            return extractFromJson(data, cityName);
        default:
            break;
    }
}

function extractFromJson(data, cityName) {

    /*
    var mapDataLocations = [{"Latitude":53.4051319444,"Longitude":-2.9971558333,"LocalTitle":"Pier Head Ferry Terminal",
    "LocalInformation":"...",
    "AvailableBikesCount":8,"FreeLocksCount":2,"StatusPercentageNumber":6,"StationSizeShortcut":"m"},
    */

    var startMarker = 'mapDataLocations = [';
    var endMarker = '}];';
    var start = data.indexOf(startMarker) + startMarker.length - 1;
    var end = data.indexOf(endMarker, start) + 2;
    data = data.substr(start, end - start);
    data = JSON.parse(data);

    var stations = [];

    var index = 1;
    var idxStation = 0;
    
    data.forEach(function visitMarker(marker) {
        var station = {
            id: index++,
            name: marker.LocalTitle,
            address: '',
            city: cityName,
            lat: marker.Latitude,
            lng: marker.Longitude,
            status: 1,
            bikes: marker.AvailableBikesCount,
            freeDocks: marker.FreeLocksCount,
            totalDocks: marker.AvailableBikesCount + marker.FreeLocksCount
        }
        stations[idxStation++] = station;
    });
    return JSON.stringify(stations);
}
