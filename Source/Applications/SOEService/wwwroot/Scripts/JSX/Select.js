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
var _ = require("lodash");
var Select = (function (_super) {
    __extends(Select, _super);
    function Select(props) {
        var _this = _super.call(this, props) || this;
        _this.state = {
            options: props.options,
            dynamicColumns: null,
            value: props.value
        };
        return _this;
    }
    Select.prototype.componentDidMount = function () {
        this.setState({ dynamicColumns: this.state.options.map(function (o, i) {
                return React.createElement("option", { key: o }, o);
            }) });
    };
    Select.prototype.componentWillReceiveProps = function (nextProps) {
        if (!(_.isEqual(this.props, nextProps)))
            this.setState({
                options: nextProps.options,
                dynamicColumns: this.state.options.map(function (o, i) {
                    return React.createElement("option", { key: o }, o);
                }),
                value: nextProps.value
            });
    };
    Select.prototype.onChange = function (event) {
        this.setState({ value: event.target.value });
        if (this.props.onChange != undefined)
            this.props.onChange(event.target.value);
    };
    Select.prototype.render = function () {
        return (React.createElement("div", { className: "form-group" },
            this.props.formLabel != undefined ? (React.createElement("label", null, this.props.formLabel)) : (null),
            React.createElement("select", { className: "form-control", value: this.state.value, onChange: this.onChange.bind(this) }, this.state.dynamicColumns)));
    };
    return Select;
}(React.Component));
exports.default = Select;
//# sourceMappingURL=Select.js.map