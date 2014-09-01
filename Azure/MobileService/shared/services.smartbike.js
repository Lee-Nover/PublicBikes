exports.serviceName = "smartbike";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    switch (cityName) {
        case 'stockholm':
            return 'https://secure.citybikes.se/maps/a/grs';
        case 'antwerpen':
            return "https://www.velo-antwerpen.be/availability_map/getJsonObject";
        case 'mexicocity':
            return "https://www.ecobici.df.gob.mx/availability_map/getJsonObject";
        case "milano":
            return "https://www.bikemi.com/it/mappa-stazioni.aspx";
        default:
            return '';
    }
}

exports.extractData = function (data, cityName) {
    switch (cityName) {
        case 'stockholm':
            return extractFromXML(data, cityName);
        case 'antwerpen':
        case 'mexicocity':
            return extractFromJsonMX(data, cityName);
        case 'milano':
            return extractFromJsonMilano(data, cityName);
        default:
            break;
    }
}

function extractFromXML(data, cityName) {
    /*
    <racks>
        <station>
            <rack_id>1</rack_id>
            <description>[1] Allmänna gränd Gröna Lund</description>
            <longitude>18.0948995</longitude>
            <latitude>59.3242468</latitude>
            <color>red</color>
            <last_update>2013-11-22 12:00:01</last_update>
            <online>0</online>
        </station>
    */

    var stations = [];
    var idxStation = 0;
    var cheerio = require('cheerio');
    var filterName = /^(\[?\]?\s?\d+\]?\[?(\s?)+)/;
    $ = cheerio.load(data, { ignoreWhitespace: true, xmlMode: true} ); // load the html nodes
    $('station').each(function(i, item) {
        var co = $(item);
        var station = {
            id: parseInt($('rack_id', co).text()),
            name: $('description', co).text().replace(filterName, ''),
            //address: address,
            city: cityName,
            lat: parseFloat($('latitude', co).text()),
            lng: parseFloat($('longitude', co).text()),
            status: parseInt($('online', co).text())/*,
            bikes: null,
            freeDocks: null,
            totalDocks: null*/
        }
        stations[idxStation++] = station;
    });

    return JSON.stringify(stations);
}

function extractFromJsonMilano(data, cityName) {
    var startMarker = 'markerOptions":[';
    var endMarker = '}],';
    var start = data.indexOf(startMarker) + startMarker.length - 1;
    var end = data.indexOf(endMarker, start) + 2;
    data = data.substr(start, end - start);
    data = JSON.parse(data);

    var stations = [];

    var index = 1;
    var idxStation = 0;
    var cheerio = require('cheerio');
    var rxGetNum = /\d+/;
    
    data.forEach(function visitMarker(marker) {
        var info = marker.info;
        $ = cheerio.load(info); // load the html nodes
        var avail = $('li');
        var available = parseInt(rxGetNum.exec($($(avail)[0]).text()));
        var free = parseInt(rxGetNum.exec($($(avail)[1]).text()));
        var title = marker.title;
        var idxTitle = title.indexOf('-');
        if (idxTitle > 0) {
            index = parseInt(title.substr(0, idxTitle-1).trim());
            title = title.substr(idxTitle+1).trim();
        }
        var station = {
            id: index++,
            name: title,
            address: '',
            city: cityName,
            lat: marker.position.lat,
            lng: marker.position.lng,
            status: 1,
            bikes: available,
            freeDocks: free,
            totalDocks: available + free
        }
        stations[idxStation++] = station;
    });
    return JSON.stringify(stations);
}

function extractFromJsonMX(data, cityName) {
    var stationList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    var filterName = /^([\d\s-])*/;
    stationList.forEach(function visitStation(s) {
        var station = {
            id: s.id,
            name: s.name.replace(filterName, ''),
            address: s.address.replace(filterName, ''),
            city: cityName,
            lat: s.lat,
            lng: s.lon,
            status: s.status == "OPN" ? 1 : 0,
            bikes: parseInt(s.bikes),
            freeDocks: parseInt(s.slots),
            totalDocks: parseInt(s.bikes) + parseInt(s.slots)
        }
        stations[idxStation++] = station;    
    });
    return JSON.stringify(stations);
}
