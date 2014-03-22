exports.serviceName = "nextbike";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    return 'http://nextbike.net/maps/nextbike-official.xml?city=' + cityName;
}

exports.extractData = function (data, cityName) {
    return extractFromXML(data, cityName);
}

function extractFromXML(data, cityName) {
    /*
    <markers>
      <country lat="50.7086" lng="10.6348" zoom="5" name="nextbike Germany" hotline="+493069205046" domain="de" country="DE" country_name="Germany">
        <city uid="201" lat="48.1726" lng="12.8311" zoom="14" maps_icon="" alias="burghausen" break="0" name="Burghausen">
          <place uid="130231" lat="48.174185" lng="12.828019" name="Bahnhof" spot="1" number="4540" bikes="5+" bike_numbers="00537,00547,00545,00546,00561" />
          <place uid="130234" lat="48.159541" lng="12.833249" name="Stadtplatz" spot="1" number="4542" bikes="5+" bike_numbers="00556,00538,00542,00560,00562" />
          <place uid="130235" lat="48.164175" lng="12.832696" name="Burg" spot="1" number="4541" bikes="4" bike_numbers="00543,00539,00564,00555" />
        </city>
      </country>
    </markers>
    */


    var stations = [];
    var idxStation = 0;
    var cheerio = require('cheerio');
    $ = cheerio.load(data, { ignoreWhitespace: true, xmlMode: true} ); // load the html nodes
    $('place').each(function(i, item) {
        var co = $(item);
        var station = {
            id: parseInt(co.attr('number')),
            name: co.attr('name'),
            //address: address,
            city: cityName,
            lat: parseFloat(co.attr('lat')),
            lng: parseFloat(co.attr('lng')),
            status: 1,
            bikes: parseInt(co.attr('bikes')), // parseInt handles '+5'
            freeDocks: parseInt(co.attr('spot') || '1'),
            totalDocks: 0
        }
        station.totalDocks = station.freeDocks;
        stations[idxStation++] = station;
    });

    return JSON.stringify(stations);
}
