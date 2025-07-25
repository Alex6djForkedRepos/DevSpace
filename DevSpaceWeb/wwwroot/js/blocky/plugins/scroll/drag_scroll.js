/*! For license information please see index.js.LICENSE.txt */


export class ScrollOptions {

    workspace_;

    /** Bound event listener for the scroll wheel event. */

    wheelEvent_;

    /**

     * Constructor for ScrollOptions plugin.

     *

     * @param workspace The workspace that the plugin will

     *     be added to.

     */

    constructor(workspace) {
        this.workspace_ = workspace;
    }

    /**

     * Initialize plugin with optional options. If no options are provided, both
     * 
     * plugin features are enabled with default settings. The plugin is configured

     * here as a convenience. See the README for more information on configuring

     * the plugin after initialization.

     *

     * @param root0

     * @param root0.enableWheelScroll

     * @param root0.enableEdgeScroll

     * @param root0.edgeScrollOptions

     *

     *  options The

     * configuration options for the plugin. `enableWheelScroll` and

     * `enableEdgeScroll` are both true by default and control whether the

     * behavior to scroll with the mouse wheel while dragging and scroll when a

     * block is near the edge of the workspace are enabled, respectively.

     * `edgeScrollOptions` is an optional configuration for the edge scrolling

     * behavior. See `ScrollBlockDrager.updateOptions` for more details.

     */

    init({ enableWheelScroll, enableEdgeScroll, edgeScrollOptions, }) {
        this.enableWheelScroll = enableWheelScroll;
        this.enableEdgeScroll = enableEdgeScroll;
        this.edgeScrollEnabled = edgeScrollOptions;
    }

    /**

     * Enables scrolling with mousewheel during block drag.

     */

    enableWheelScroll() {
        this.enableWheelScroll = true;
    }

    /**

     * Disables scrolling with mousewheel during block drag.

     */

    disableWheelScroll() {
        this.enableWheelScroll = false;
    }

    /**

     * Enables scrolling when block is dragged near edge.

     */

    enableEdgeScroll() {
        this.enableEdgeScroll = true;
    }

    /**

     * Disables scrolling when block is dragged near edge.

     */

    disableEdgeScroll() {
        this.enableEdgeScroll = false;
    }

    /**

     * Updates edge scroll options. See ScrollBlockDragger for specific settings.

     * Any values left unspecified will not be overwritten and will retain their

     * previous values.

     *

     * @param options Edge scroll options.

     */

    updateEdgeScrollOptions(options) {

    }

    /**

     * Scrolls the workspace with the mousewheel while a block is being dragged.

     * Translates the currently dragged block as the user scrolls the workspace,

     * so that the block does not appear to move.

     *

     * @param e Mouse wheel event.

     */

    onMouseWheel_(e) {

    }

}


!function (t, e) { if ("object" == typeof exports && "object" == typeof module) module.exports = e(require("blockly/core")); else if ("function" == typeof define && define.amd) define(["blockly/core"], e); else { var o = "object" == typeof exports ? e(require("blockly/core")) : e(t.Blockly); for (var s in o) ("object" == typeof exports ? exports : t)[s] = o[s] } }(this, (t => (() => { "use strict"; var e = { 370: e => { e.exports = t } }, o = {}; function s(t) { var i = o[t]; if (void 0 !== i) return i.exports; var r = o[t] = { exports: {} }; return e[t](r, r.exports, s), r.exports } s.d = (t, e) => { for (var o in e) s.o(e, o) && !s.o(t, o) && Object.defineProperty(t, o, { enumerable: !0, get: e[o] }) }, s.o = (t, e) => Object.prototype.hasOwnProperty.call(t, e), s.r = t => { "undefined" != typeof Symbol && Symbol.toStringTag && Object.defineProperty(t, Symbol.toStringTag, { value: "Module" }), Object.defineProperty(t, "__esModule", { value: !0 }) }; var i = {}; s.r(i), s.d(i, { ScrollBlockDragger: () => d, ScrollMetricsManager: () => l, ScrollOptions: () => p, isAutoScrollable: () => h, isCacheable: () => n }); var r = s(370); class l extends r.MetricsManager { constructor() { super(...arguments), this.useCachedContentMetrics = !1, this.contentMetrics = null } getContentMetrics() { return this.useCachedContentMetrics && this.contentMetrics || (this.contentMetrics = super.getContentMetrics()), this.contentMetrics } } function n(t) { return void 0 !== t.useCachedContentMetrics } const c = t => { const e = t.svgBlockCanvas_.getAttribute("transform"); if (!e) throw new Error("svgBlockCanvas has no attribute 'transform'"); const o = e.split(","), s = Number(o[0].split("(")[1]), i = Number(o[1].split(")")[0]); return new r.utils.Coordinate(s, i) }; class a { constructor(t, e) { this.activeScrollVector_ = new r.utils.Coordinate(0, 0), this.animationFrameId_ = 0, this.lastTime_ = Date.now(), this.shouldAnimate_ = !1, this.workspace_ = t, this.dragger = e } stopAndDestroy() { this.activeScrollVector_ = new r.utils.Coordinate(0, 0), this.shouldAnimate_ = !1, cancelAnimationFrame(this.animationFrameId_), this.animationFrameId_ = 0 } nextAnimationStep_(t) { if (this.shouldAnimate_) { const e = t - this.lastTime_; this.lastTime_ = t, e > 0 && this.scrollTick_(e), this.animationFrameId_ = requestAnimationFrame((t => this.nextAnimationStep_(t))) } } scrollTick_(t) { const e = this.activeScrollVector_.x * t, o = this.activeScrollVector_.y * t; this.scrollWorkspaceWithBlock(e, o) } scrollWorkspaceWithBlock(t, e) { const o = c(this.workspace_), s = this.workspace_.getMetricsManager(); if (!n(s)) return void console.warn("MetricsManager must be able to cache metrics in order to use AutoScroll"); s.useCachedContentMetrics = !0; const i = this.workspace_.scrollX + t, r = this.workspace_.scrollY + e; this.workspace_.scroll(i, r), s.useCachedContentMetrics = !1; const l = c(this.workspace_), a = l.x - o.x, h = l.y - o.y; (a || h) && this.dragger.moveBlockWhileDragging(a, h) } updateProperties(t) { this.activeScrollVector_ = t, this.shouldAnimate_ = !0, 0 == this.animationFrameId_ && (this.lastTime_ = Date.now(), this.nextAnimationStep_(this.lastTime_)) } } function h(t) { return "function" == typeof t.getBoundingRectangle } const u = { slowBlockSpeed: .28, fastBlockSpeed: 1.4, slowBlockStartDistance: 0, fastBlockStartDistance: 50, oversizeBlockThreshold: .85, oversizeBlockMargin: 15, slowMouseSpeed: .5, fastMouseSpeed: 1.6, slowMouseStartDistance: 0, fastMouseStartDistance: 35 }; class d extends r.dragging.Dragger { constructor() { super(...arguments), this.scrollDelta_ = new r.utils.Coordinate(0, 0), this.dragDelta_ = new r.utils.Coordinate(0, 0), this.scrollDirections_ = ["top", "bottom", "left", "right"], this.SCROLL_DIRECTION_VECTORS_ = { top: new r.utils.Coordinate(0, 1), bottom: new r.utils.Coordinate(0, -1), left: new r.utils.Coordinate(1, 0), right: new r.utils.Coordinate(-1, 0) }, this.activeAutoScroll_ = null } moveBlockWhileDragging(t, e) { if (!h(this.draggable)) return; this.scrollDelta_.x -= t, this.scrollDelta_.y -= e; const o = r.utils.Coordinate.sum(this.scrollDelta_, this.dragDelta_), s = this.pixelsToWorkspaceUnits(o), i = r.utils.Coordinate.sum(this.startLoc, s); this.draggable.drag(i) } onDrag(t, e) { const o = r.utils.Coordinate.sum(this.scrollDelta_, e); super.onDrag(t, o), this.dragDelta_ = e, d.edgeScrollEnabled && this.scrollWorkspaceWhileDragging_(t) } onDragEnd(t) { super.onDragEnd(t), this.stopAutoScrolling() } scrollWorkspaceWhileDragging_(t) { if (!h(this.draggable)) return; const e = r.utils.svgMath.screenToWsCoordinates(this.workspace, new r.utils.Coordinate(t.clientX, t.clientY)), o = { top: [], bottom: [], left: [], right: [] }, s = this.workspace.getMetricsManager().getViewMetrics(!0); this.computeBlockCandidateScrolls_(o, s, e), this.computeMouseCandidateScrolls_(o, s, e); const i = this.getOverallScrollVector_(o); r.utils.Coordinate.equals(i, new r.utils.Coordinate(0, 0)) ? this.stopAutoScrolling() : (this.activeAutoScroll_ = this.activeAutoScroll_ || new a(this.workspace, this), this.activeAutoScroll_.updateProperties(i)) } getOverallScrollVector_(t) { let e = new r.utils.Coordinate(0, 0); for (const o of this.scrollDirections_) { const s = t[o].reduce(((t, e) => t && r.utils.Coordinate.magnitude(t) > r.utils.Coordinate.magnitude(e) ? t : e), new r.utils.Coordinate(0, 0)); e = r.utils.Coordinate.sum(e, s) } return e } computeBlockCandidateScrolls_(t, e, o) { const s = this.getBlockBoundsOverflows_(e, o); for (const e of this.scrollDirections_) { const o = s[e]; if (o > d.options.slowBlockStartDistance) { const s = o > d.options.fastBlockStartDistance ? d.options.fastBlockSpeed : d.options.slowBlockSpeed, i = this.SCROLL_DIRECTION_VECTORS_[e].clone().scale(s); t[e].push(i) } } } computeMouseCandidateScrolls_(t, e, o) { const s = this.getMouseOverflows_(e, o); for (const e of this.scrollDirections_) { const o = s[e]; if (o > d.options.slowMouseStartDistance) { const s = o > d.options.fastMouseStartDistance ? d.options.fastMouseSpeed : d.options.slowMouseSpeed, i = this.SCROLL_DIRECTION_VECTORS_[e].clone().scale(s); t[e].push(i) } } } getBlockBoundsOverflows_(t, e) { if (!h(this.draggable)) return { top: 0, bottom: 0, left: 0, right: 0 }; const o = this.draggable.getBoundingRectangle(); return o.bottom - o.top > t.height * d.options.oversizeBlockThreshold && (o.top = Math.max(o.top, e.y - d.options.oversizeBlockMargin), o.bottom = Math.min(o.bottom, e.y + d.options.oversizeBlockMargin)), o.right - o.left > t.width * d.options.oversizeBlockThreshold && (o.left = Math.max(o.left, e.x - d.options.oversizeBlockMargin), o.right = Math.min(o.right, e.x + d.options.oversizeBlockMargin)), { top: t.top - o.top, bottom: -(t.top + t.height - o.bottom), left: t.left - o.left, right: -(t.left + t.width - o.right) } } getMouseOverflows_(t, e) { return { top: t.top - e.y, bottom: -(t.top + t.height - e.y), left: t.left - e.x, right: -(t.left + t.width - e.x) } } stopAutoScrolling() { this.activeAutoScroll_ && this.activeAutoScroll_.stopAndDestroy(), this.activeAutoScroll_ = null } } d.edgeScrollEnabled = !0, d.options = u, d.updateOptions = function (t) { d.options = Object.assign(Object.assign({}, d.options), t) }, d.resetOptions = function () { d.options = u }, r.registry.register(r.registry.Type.BLOCK_DRAGGER, "ScrollBlockDragger", d); class p { constructor(t) { this.wheelEvent_ = null, this.workspace_ = t } init({ enableWheelScroll: t = !0, enableEdgeScroll: e = !0, edgeScrollOptions: o } = { enableWheelScroll: !0, enableEdgeScroll: !0, edgeScrollOptions: void 0 }) { t ? this.enableWheelScroll() : this.disableWheelScroll(), d.edgeScrollEnabled = e, o && d.updateOptions(o) } enableWheelScroll() { if (this.wheelEvent_) return; const t = this.workspace_.getInjectionDiv().getElementsByClassName("blocklyBlockDragSurface")[0]; if (!t) throw new Error("Can't attach wheel listener to nonexistent drag surface"); this.wheelEvent_ = r.browserEvents.conditionalBind(t, "wheel", this, this.onMouseWheel_) } disableWheelScroll() { this.wheelEvent_ && (r.browserEvents.unbind(this.wheelEvent_), this.wheelEvent_ = null) } enableEdgeScroll() { d.edgeScrollEnabled = !0 } disableEdgeScroll() { d.edgeScrollEnabled = !1 } updateEdgeScrollOptions(t) { d.updateOptions(t) } onMouseWheel_(t) { const e = this.workspace_.options.moveOptions && this.workspace_.options.moveOptions.wheel, o = this.workspace_.getGesture(t), s = this.workspace_.getMetricsManager(); if (!n(s)) return void console.warn("MetricsManager must be able to cache metrics in order to use AutoScroll"); const i = null == o ? void 0 : o.getCurrentDragger(); if (!(e && o && i instanceof d)) return; const l = r.browserEvents.getScrollDeltaPixels(t); if (t.shiftKey) { const t = l.x; l.x = l.y, l.y = t } const a = this.workspace_.scrollX - l.x, h = this.workspace_.scrollY - l.y, u = c(this.workspace_); s.useCachedContentMetrics = !0, this.workspace_.scroll(a, h), s.useCachedContentMetrics = !1; const p = c(this.workspace_), g = p.x - u.x, _ = p.y - u.y; (g || _) && (i.moveBlockWhileDragging(g, _), t.preventDefault()) } } return i })()));
