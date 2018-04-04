"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
var React = require("react");
var ReactDOM = require("react-dom");
var SOEService_1 = require("./../Services/SOEService");
var IncidentGroup_1 = require("./IncidentGroup");
var createBrowserHistory_1 = require("history/createBrowserHistory");
var queryString = require("query-string");
var moment = require("moment");
var WaveformViewer = (function (_super) {
    __extends(WaveformViewer, _super);
    function WaveformViewer(props) {
        var _this = _super.call(this, props) || this;
        _this.soeservice = new SOEService_1.default();
        _this.history = createBrowserHistory_1.default();
        var query = queryString.parse(_this.history['location'].search);
        _this.state = {
            IncidentID: (query['IncidentID'] != undefined ? query['IncidentID'] : 0),
            StartDate: query['StartDate'],
            EndDate: query['EndDate']
        };
        _this.dynamicRows = [React.createElement("div", { key: "fake" })];
        _this.history['listen'](function (location, action) {
            var query = queryString.parse(_this.history['location'].search);
            _this.setState({
                IncidentID: (query['IncidentID'] != undefined ? query['IncidentID'] : 0),
                StartDate: query['StartDate'],
                EndDate: query['EndDate']
            }, function () {
                _this.getData(_this.state);
            });
        });
        return _this;
    }
    WaveformViewer.prototype.getData = function (state) {
        var _this = this;
        this.soeservice.getIncidentGroups(state).then(function (data) {
            var orderedData = data[1].filter(function (x) { return data[0].map(function (y) { return y.MeterID; }).indexOf(x.ID) >= 0; }).map(function (x) { return data[0][data[0].map(function (y) { return y.MeterID; }).indexOf(x.ID)]; });
            if (_this.state.StartDate == null) {
                var startUnix = Math.min.apply(Math, orderedData.map(function (x) { return moment(x.StartTime).unix() + (x.StartTime.indexOf('.') >= 0 ? parseFloat('.' + x.StartTime.split('.')[1]) : 0); }));
                var startString = '';
                if (startUnix.toString().indexOf('.') >= 0)
                    startString = moment.unix(parseInt(startUnix.toString().split('.')[0])).format('YYYY-MM-DDTHH:mm:ss') + '.' + startUnix.toString().split('.')[1];
                else
                    startString = moment.unix(startUnix).format('YYYY-MM-DDTHH:mm:ss');
                _this.setState({ StartDate: startString });
            }
            if (_this.state.EndDate == null) {
                var endUnix = Math.max.apply(Math, orderedData.map(function (x) { return moment(x.EndTime).unix() + (x.EndTime.indexOf('.') >= 0 ? parseFloat('.' + x.EndTime.split('.')[1]) : 0); }));
                var endString = '';
                if (endUnix.toString().indexOf('.') >= 0)
                    endString = moment.unix(parseInt(endUnix.toString().split('.')[0])).format('YYYY-MM-DDTHH:mm:ss') + '.' + endUnix.toString().split('.')[1];
                else
                    endString = moment.unix(endUnix).format('YYYY-MM-DDTHH:mm:ss');
                _this.setState({ EndDate: endString });
            }
            var parentIds = orderedData.map(function (x) { return x.ParentID; });
            var meterIds = orderedData.map(function (x) { return x.MeterID; });
            _this.meterList = orderedData.map(function (x) {
                return React.createElement("a", { key: '#' + x.MeterName, onClick: function (e) { return _this.goToDiv(x.MeterName); } }, x.MeterName);
            });
            _this.timeList = data[2].map(function (x, i) { return React.createElement("button", { key: i + x, onClick: function (e) { return _this.goToTime(x); }, className: "btn" }, i + 1); });
            _this.dynamicRows = orderedData.map(function (d, i) {
                return React.createElement(IncidentGroup_1.default, { key: d["MeterID"], incidentId: d["ID"], circuitId: d["CircuitID"], meterId: d["MeterID"], meterName: d["MeterName"], startDate: _this.state.StartDate, endDate: _this.state.EndDate, pixels: window.innerWidth, stateSetter: _this.stateSetter.bind(_this) });
            });
            _this.forceUpdate();
        });
    };
    WaveformViewer.prototype.goToTime = function (timeStamp) {
        var milliseconds = this.getMillisecondTime(timeStamp.Timestamp);
        var startDate = this.getDateString(milliseconds - 20);
        var endDate = this.getDateString(milliseconds + 20);
        this.stateSetter({
            StartDate: startDate,
            EndDate: endDate
        });
    };
    WaveformViewer.prototype.goToDiv = function (meterName) {
        var element = document.getElementById(meterName);
        if (element) {
            if (!/^(?:a|select|input|button|textarea)$/i.test(element.tagName)) {
                element.tabIndex = -1;
            }
            element.focus();
        }
    };
    WaveformViewer.prototype.componentDidMount = function () {
        this.getData(this.state);
        window.addEventListener("resize", this.handleScreenSizeChange.bind(this));
        window.addEventListener("keyup", this.moveCharts.bind(this));
    };
    WaveformViewer.prototype.componentWillUnmount = function () {
        $(window).off('resize');
        $(window).off('keyup');
    };
    WaveformViewer.prototype.handleScreenSizeChange = function () {
        var _this = this;
        clearTimeout(this.resizeId);
        this.resizeId = setTimeout(function () {
            _this.getData(_this.state);
        }, 500);
    };
    WaveformViewer.prototype.render = function () {
        return (React.createElement("div", { className: "screen", style: { height: window.innerHeight - 60 } },
            React.createElement("div", { className: "vertical-menu" }, this.meterList),
            React.createElement("div", { className: "waveform-viewer", style: { width: window.innerWidth - 150 } },
                React.createElement("div", { className: "horizontal-row" },
                    React.createElement("button", { className: "btn", onClick: this.resetZoom.bind(this) }, "Reset"),
                    React.createElement("span", { style: { marginLeft: '3px', marginRight: '3px' } }, "Points of Interest:"),
                    this.timeList),
                React.createElement("div", { className: "list-group", style: { maxHeight: window.innerHeight - 100, overflowY: 'auto' } }, this.dynamicRows))));
    };
    WaveformViewer.prototype.stateSetter = function (obj) {
        var _this = this;
        this.setState(obj, function () { return _this.history['push']('CommonAggregateView.cshtml?' + queryString.stringify(_this.state, { encode: false })); });
    };
    WaveformViewer.prototype.collapseAllPanels = function () {
        $('.in').removeClass('in');
    };
    WaveformViewer.prototype.resetZoom = function () {
        this.history['push']('CommonAggregateView.cshtml?' + queryString.stringify({ IncidentID: this.state.IncidentID }, { encode: false }));
    };
    WaveformViewer.prototype.moveCharts = function () {
    };
    WaveformViewer.prototype.getMillisecondTime = function (date) {
        var milliseconds = moment.utc(date).valueOf();
        var millisecondsFractionFloat = parseFloat((date.toString().indexOf('.') >= 0 ? '.' + date.toString().split('.')[1] : '0')) * 1000;
        return milliseconds + millisecondsFractionFloat - Math.floor(millisecondsFractionFloat);
    };
    WaveformViewer.prototype.getDateString = function (float) {
        var date = moment.utc(float).format('YYYY-MM-DDTHH:mm:ss.SSS');
        var millisecondFraction = parseInt((float.toString().indexOf('.') >= 0 ? float.toString().split('.')[1] : '0'));
        return date + millisecondFraction.toString();
    };
    return WaveformViewer;
}(React.Component));
ReactDOM.render(React.createElement(WaveformViewer, null), document.getElementById('bodyContainer'));
//# sourceMappingURL=WaveformViewer.js.map