exports.serviceName = "B-cycle";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    return "http://" + cityName + ".bcycle.com/home.aspx";
}

exports.extractData = function (data, cityName) {
    var startMarker = "function LoadKiosks()";
    var endMarker = "</script>";
    var start = data.indexOf(startMarker) + startMarker.length - 1;
    var end = data.indexOf(endMarker, start) + 1;
    data = data.substr(start, end - start);
    data = parseData(data, cityName);
    data = JSON.stringify(data);
    return data;
}

function parseData(data, cityName) {
    /*
             
    var icon = '../Portals/10/images/maps/marker-outofservice.png';
    var back = 'infowin-unavail';
    var point = new google.maps.LatLng(40.01630, -105.28230);
    kioskpoints.push(point);
    var marker = new createMarker(point, "<div class='location'><strong>10th & Walnut</strong><br />10th St. & Walnut St.<br />Boulder, CO 80302</div><div class='avail'>Bikes available: <strong>5</strong><br />Docks available: <strong>6</strong></div><div></div>", icon, back);
    markers.push(marker);

    */

    var CCoordStr = 'point = new google.maps.LatLng(';
    var CDataStr = 'createMarker(point, "';
    var CDataEndStr = '", icon, back);';
    var cheerio = require('cheerio');
    
    var stations = [];
    var index = 1;
    var idxStation = 0;
    
    var coordPos = data.indexOf(CCoordStr);
    
    while (coordPos > -1)
    {
        coordPos += CCoordStr.length;
        var coordEndPos = data.indexOf(');', coordPos);
        var coordStr = data.substring(coordPos, coordEndPos);
        var coords = coordStr.split(',');
        
        dataPos = data.indexOf(CDataStr, coordEndPos) + CDataStr.length;
        var dataEndPos = data.indexOf(CDataEndStr, dataPos);
        var dataStr = data.substring(dataPos, dataEndPos);
        dataStr = dataStr.replace('&', '&amp;');
        dataStr = '<div>' + dataStr + '</div>';
        $ = cheerio.load(dataStr); // load the html nodes

        // html
        // read the name and address and availability from html
        // name = //div[@class='location']/strong/text()
        // address = //div[@class='location']/text()
        var loc = $('div[class=location]');
        var name = $($(loc).find('strong')).text();
        var allText = loc.text();
        var address = allText.substring(name.length, allText.length);
        
        // bikes = //div[@class='avail']/strong[1]/text()
        // docks = //div[@class='avail']/strong[2]/text()
        var avail = $('div[class=avail]').find('strong');
        var available = parseInt($($(avail)[0]).text());
        var free = parseInt($($(avail)[1]).text());

        var station = {
            id: index++,
            name: name,
            address: address,
            city: cityName,
            lat: parseFloat(coords[0].trim()),
            lng: parseFloat(coords[1].trim()),
            status: 1,
            bikes: available,
            freeDocks: free,
            totalDocks: available + free
        }
        stations[idxStation++] = station;

        coordPos = data.indexOf(CCoordStr, dataPos);
    }

    return stations;
}