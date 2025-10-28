/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./InlineFrame/index.ts":
/*!******************************!*\
  !*** ./InlineFrame/index.ts ***!
  \******************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   InlineFrame: () => (/* binding */ InlineFrame)\n/* harmony export */ });\nclass InlineFrame {\n  constructor() {\n    // Do nothing.\n  }\n  init(context, notifyOutputChanged, state, container) {\n    context.mode.trackContainerResize(true);\n    this.container = container;\n    this.element = document.createElement(\"iframe\");\n    this.element.width = context.mode.allocatedWidth.toString();\n    this.element.height = context.mode.allocatedHeight.toString();\n    this.element.style.boxSizing = \"border-box\";\n    if (context.parameters.URL.raw) {\n      this.element.src = context.parameters.URL.raw;\n      if (this.container.contains(this.element) == false) {\n        this.container.append(this.element);\n      }\n    } else {\n      if (this.container.contains(this.element) == true) {\n        this.container.removeChild(this.element);\n      }\n    }\n  }\n  updateView(context) {\n    this.element.width = context.mode.allocatedWidth.toString();\n    this.element.height = context.mode.allocatedHeight.toString();\n    if (context.parameters.URL.raw) {\n      this.element.src = context.parameters.URL.raw;\n      if (this.container.contains(this.element) == false) {\n        this.container.append(this.element);\n      }\n    } else {\n      if (this.container.contains(this.element) == true) {\n        this.container.removeChild(this.element);\n      }\n    }\n  }\n  getOutputs() {\n    return {};\n  }\n  destroy() {\n    // Do nothing.\n  }\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./InlineFrame/index.ts?\n}");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The require scope
/******/ 	var __webpack_require__ = {};
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	var __webpack_exports__ = {};
/******/ 	__webpack_modules__["./InlineFrame/index.ts"](0, __webpack_exports__, __webpack_require__);
/******/ 	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = __webpack_exports__;
/******/ 	
/******/ })()
;
if (window.ComponentFramework && window.ComponentFramework.registerControl) {
	ComponentFramework.registerControl('GGMS.InlineFrame', pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.InlineFrame);
} else {
	var GGMS = GGMS || {};
	GGMS.InlineFrame = pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.InlineFrame;
	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = undefined;
}