var port = process.env.port || 1337;
var express = require('express');
var app = module.exports = express();
app.use(express.compress());
var response;

app.get('/', function (req, resp) {
    resp.type('text/plain'); // set content-type
    resp.send('use the path as:  server/serviceName/city/{stationId} where stationId is optional.'); // send text response
});

app.get('/:service/:city/:id?', function (req, resp) {
    var service = req.params.service;
    var city = req.params.city;
    var id = req.params.id;
    var api = require('./api/stations');
    response = resp;
    api.get(req, resp);
});

process.on('uncaughtException', function (err) {
  console.error(err);
  response.send(500, err);
});

app.listen(port);