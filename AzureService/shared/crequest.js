var request = require('request');
var zlib = require('zlib');

emitex = function (evt, arg1) {
    var emit = this.emitold;
    var self = this;
    switch (evt) {
        case 'response':
            var encoding = arg1.headers['content-encoding'];
            if (typeof encoding === 'string')
                encoding = encoding.toLowerCase();
            if (encoding == 'gzip')
                this.decoder = zlib.createGunzip();
            else if (encoding == 'deflate')
                this.decoder = zlib.createInflate();
            if (this.decoder) {
                this.decoder.on('data', function (data) {
                    emit.call(self, 'data', data);
                });
                this.decoder.on('end', function () {
                    emit.call(self, 'end');
                });
                this.decoder.on('finish', function () {
                    emit.call(self, 'end');
                });
            }
            break;

        case 'data':
            if (this.decoder) {
                this.decoder.write(arg1);
                return;
            }
            break;

        case 'end':
            if (this.decoder) {
                this.decoder.end();
                return;
            }
            break;

        default:
            break;
    }
    emit.apply(self, arguments);
}

function crequest(uri, options, callback) {
    var req = request(uri, options, callback);
    req.headers = req.headers || {};
    req.setHeader('accept-encoding', 'gzip,deflate', false);
    req.emitold = req.emit;
    req.emit = emitex;
    return req;
}

module.exports = crequest;