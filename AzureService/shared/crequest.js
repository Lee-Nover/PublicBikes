var request = require('request');
//var util = require('util');
var zlib = require('zlib');

/*function CRequest(options) {
    request.Request.call(this);
    this.setHeader('accept-encoding', 'gzip,deflate', false);

}

util.inherits(CRequest, request.Request);*/

exports = module.exports = function (options, callback) {
    
    var headers = {
        //'accept-charset' : 'ISO-8859-1,utf-8;q=0.7,*;q=0.3',
        'accept-language': 'en-US,en;q=0.8',
        'accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        'accept-encoding': 'gzip,deflate'
    };

    if (typeof options === 'string') {
        options = {
            url: options,
            headers: headers
        };
    } else {
        options.headers['accept-encoding'] = 'gzip,deflate';
    }

    var response = null;
    var handleError = function (e) {
        if (callback)
            callback(e, response);
        else throw e;
    }
    var chunks = [];
    var stream = require('stream');
    var outStream = new stream.Stream();
    outStream.writable = true;
    outStream.write = function (sdata) {
        chunks.push(sdata);
        return true;
    };
    outStream.end = function (data) {
        if (data)
            chunks.push(data);
        var result = Buffer.concat(chunks).toString();
        if (callback)
            callback(null, response, result);
    };

    var req = request(options);
    req.on('response', function (res) {
        response = res;
        if (res.statusCode && res.statusCode !== 200) {
            handleError(new Error('Status not 200'));
            return;
        }

        var encoding = res.headers['content-encoding'];
        try {
            if (encoding == 'gzip') {
                res.pipe(zlib.createGunzip()).pipe(outStream);
            } else if (encoding == 'deflate') {
                res.pipe(zlib.createInflate()).pipe(outStream);
            } else {
                res.pipe(outStream);
            }
        } catch (err) { handleError(err); }
    });

    req.on('error', handleError);
}