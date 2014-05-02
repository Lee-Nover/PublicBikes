exports.serviceName = "smartbike";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    return 'http://www.movesamba.com/' + cityName + '/mapaestacao.asp';
}

exports.extractData = function (data, cityName) {
    switch (cityName) {
        case 'sambarjpt':
        case 'bikesampa':
        case 'sorocaba':
            return LoadStationsFromHTML_RIO(data, cityName);
        default:
            return LoadStationsFromHTML(data, cityName);
    }
}

function LoadStationsFromHTML(s, cityName)
{
    var idxStation = 0;
    var stations = [];
    var CDataStr = 'exibirEstacaMapa(';
            
    var dataPos = s.indexOf(CDataStr);
    if (dataPos > 0)
        s = s.substr(dataPos, s.indexOf('function exibirEstacaMapa(', dataPos) - dataPos);
    dataPos = s.indexOf(CDataStr);
    while (dataPos > -1) {
        dataPos += CDataStr.length;
        var dataEndPos = s.indexOf(');', dataPos);
        var dataStr = s.substr(dataPos, dataEndPos - dataPos);
        var dataValues = eval('[' + dataStr + ']');
        // stations without the 'address' are test stations
        if (dataValues && dataValues.length > 9 && dataValues[9].trim() != '') {
            var available = parseInt(dataValues[7]);
            var free = parseInt(dataValues[8]);

            var station = {
                id: parseInt(dataValues[4]),
                name: dataValues[3],
                address: dataValues[9],
                city: cityName,
                lat: parseFloat(dataValues[0]),
                lng: parseFloat(dataValues[1]),
                status: dataValues[6] == 'EO' ? 1 : 0,
                bikes: available,
                freeDocks: free,
                totalDocks: available + free,
                connected: dataValues[7] == 'A'
            }
            stations[idxStation++] = station;
        }
        dataPos = s.indexOf(CDataStr, dataPos);
    }

    return JSON.stringify(stations);
}

function tryParseNum(s, def) {
    if (typeof s === 'number')
        return s;
    var rxNum = /\d+/;
    s = rxNum.exec(s);
    if (s == null || s === '')
        return def;
    return parseFloat(s);
}

function LoadStationsFromHTML_RIO(s, cityName)
{
    var idxStation = 0;
    var stations = [];
    var CCoordStr = 'point = new GLatLng(';
    var CDataStr = 'criaPonto(point,';

    var dataPos = s.indexOf(CCoordStr);
    if (dataPos > 0)
        s = s.substr(dataPos, s.indexOf('function criaPonto(', dataPos) - dataPos);
    dataPos = s.indexOf(CCoordStr);
    var split = '","';
    while (dataPos > -1)
    {
        dataPos += CCoordStr.length;
        // read the coordinate
        var dataEndPos = s.indexOf(');', dataPos);
        var dataStr = s.substr(dataPos, dataEndPos - dataPos);
        var dataValues = dataStr.split(',');

        var latitude = parseFloat(dataValues[0].trim());
        var longitude = parseFloat(dataValues[1].trim());

        dataPos = s.indexOf(CDataStr, dataEndPos) + CDataStr.length;
        dataEndPos = s.indexOf(') );', dataPos);
        dataStr = s.substr(dataPos, dataEndPos - dataPos);
        dataValues = eval('[' + dataStr + ']');

        var num = tryParseNum(dataValues[0]);
        if (num != null && dataValues[6] != 'I' && dataValues[7] != 'BL' && num < 900) {
            var total = tryParseNum(dataValues[8], 0);
            var available = tryParseNum(dataValues[9], 0);
            if (total < available)
                total = available;

            var station = {
                id: num,
                name: dataValues[1],
                address: dataValues[2],
                city: cityName,
                lat: latitude,
                lng: longitude,
                status: dataValues[7] == 'EO' ? 1 : 0,
                bikes: available,
                freeDocks: total - available,
                totalDocks: total,
                connected: dataValues[6] == 'A'
            }
            stations[idxStation++] = station;
        }
        dataPos = s.indexOf(CCoordStr, dataPos);
    }

    return JSON.stringify(stations);
}
