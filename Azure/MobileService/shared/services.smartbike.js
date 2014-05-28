exports.serviceName = "smartbike";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    switch (cityName) {
        case 'stockholm':
            return 'https://secure.citybikes.se/maps/a/grs';
        case 'antwerpen':
            return "https://www.velo-antwerpen.be/localizaciones/station_map.php";
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
            return extractFromHTMLAntwerp(data, cityName);
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

function extractFromHTMLAntwerp(data, cityName) {
    var stations = [];

    var index = 1;
    var idxStation = 0;
    var isMXC = cityName == 'mexicocity';
    var CCoordStr = 'point = new google.maps.LatLng(';
    if (isMXC)
        CCoordStr = 'point = new GLatLng(';
    var CDataStr = 'data:"';

    var dataPos = 0;
    var coordPos = data.indexOf(CCoordStr);
    if (coordPos > 0)
    {
        var endScriptPos = data.indexOf('</script>', coordPos);
        if (endScriptPos < coordPos)
            endScriptPos = data.length;
        data = data.substr(coordPos, endScriptPos - coordPos);
    }
    var S = require('string');
    var filterMX = /\+|\"/g;
    var toEnc = 'binary';
    if (isMXC)
        toEnc = null;

    coordPos = data.indexOf(CCoordStr);
    while (coordPos > -1)
    {
        coordPos += CCoordStr.length;
        // new GLatLng(51.199306000000000000,4.390135000000000000);
        var coordEndPos = data.indexOf(');', coordPos);
        var coordStr = data.substr(coordPos, coordEndPos - coordPos);
        var coords = coordStr.split(',');
        // antwerpen:    data:"idStation=12&addressnew=MDEyIC0gQnJ1c3NlbA==&s_id_idioma=en",
        // mexico city:  data:"idStation="+89+"&addressnew=ODkgUkVQVUJMSUNBIERFIEdVQVRFTUFMQSAtIE1PTlRFIERFIFBJRURBRA=="+"&s_id_idioma="+"es"
        dataPos = data.indexOf(CDataStr, coordEndPos) + CDataStr.length;
        var dataEndPos = data.indexOf('",', dataPos);
        var dataStr = data.substr(dataPos, dataEndPos - dataPos);
        var dataValues = dataStr.replace(filterMX, '').split('&');
        var value = dataValues[0].split('=')[1];
        index = parseInt(value);
        value = dataValues[1].substr(dataValues[1].indexOf('=') + 1);
        var b64buf = new Buffer(value, 'base64');
        var title = b64buf.toString(toEnc);
        if (isMXC)
            title = S(title).decodeHTMLEntities().s;
        var idxTitle = title.indexOf('-');
        if (idxTitle > 0)
            title = title.substr(idxTitle + 1).trim();
        coordPos = data.indexOf(CCoordStr, dataPos);

        var station = {
            id: index++,
            name: title,
            address: '',
            city: cityName,
            lat: parseFloat(coords[0]),
            lng: parseFloat(coords[1]),
            status: 1
        }
        stations[idxStation++] = station;
    };
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
