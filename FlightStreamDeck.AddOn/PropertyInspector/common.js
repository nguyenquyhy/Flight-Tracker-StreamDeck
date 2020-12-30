Array.from(document.querySelectorAll('.sdpi-file-clear')).forEach((element, index) => {
    element.onclick = function () {
        const file = document.getElementById(element.dataset.for);
        file.value = '';
        handleSdpiItemChange(file);
    }
})

function setFileLabel(wrapper, value, hasBase64) {
    const info = wrapper.querySelector('.sdpi-file-info');
    if (info) {
        if (value) {
            const s = value.split('/').pop();
            const fileName = s.length > 28
                ? s.substr(0, 10)
                + '...'
                + s.substr(s.length - 10, s.length)
                : s;
            info.textContent = (hasBase64 ? '(Embed) ' : '') + fileName;
            info.setAttribute('title', hasBase64 ? 'Embedded ' + fileName : value);
        } else {
            info.textContent = 'no file...';
        }
    }
}

/** Stream Deck software passes system-highlight color information
 * to Property Inspector. Here we 'inject' the CSS styles into the DOM
 * when we receive this information. */


function addDynamicStyles(clrs, fromWhere) {
    const node = document.getElementById('#sdpi-dynamic-styles') || document.createElement('style');
    if (!clrs.mouseDownColor) clrs.mouseDownColor = fadeColor(clrs.highlightColor, -100);
    const clr = clrs.highlightColor.slice(0, 7);
    const clr1 = fadeColor(clr, 100);
    const clr2 = fadeColor(clr, 60);
    const metersActiveColor = fadeColor(clr, -60);

    node.setAttribute('id', 'sdpi-dynamic-styles');
    node.innerHTML = `

    input[type="radio"]:checked + label span,
    input[type="checkbox"]:checked + label span {
        background-color: ${clrs.highlightColor};
    }

    input[type="radio"]:active:checked + label span,
    input[type="radio"]:active + label span,
    input[type="checkbox"]:active:checked + label span,
    input[type="checkbox"]:active + label span {
      background-color: ${clrs.mouseDownColor};
    }

    input[type="radio"]:active + label span,
    input[type="checkbox"]:active + label span {
      background-color: ${clrs.buttonPressedBorderColor};
    }

    td.selected,
    td.selected:hover,
    li.selected:hover,
    li.selected {
      color: white;
      background-color: ${clrs.highlightColor};
    }

    .sdpi-file-label > label:active,
    .sdpi-file-label.file:active,
    label.sdpi-file-label:active,
    label.sdpi-file-info:active,
    input[type="file"]::-webkit-file-upload-button:active,
    button:active {
      background-color: ${clrs.buttonPressedBackgroundColor};
      color: ${clrs.buttonPressedTextColor};
      border-color: ${clrs.buttonPressedBorderColor};
    }

    ::-webkit-progress-value,
    meter::-webkit-meter-optimum-value {
        background: linear-gradient(${clr2}, ${clr1} 20%, ${clr} 45%, ${clr} 55%, ${clr2})
    }

    ::-webkit-progress-value:active,
    meter::-webkit-meter-optimum-value:active {
        background: linear-gradient(${clr}, ${clr2} 20%, ${metersActiveColor} 45%, ${metersActiveColor} 55%, ${clr})
    }
    `;
    document.body.appendChild(node);
};

/** get a JSON property from a (dot-separated) string
 * Works on nested JSON, e.g.:
 * jsn = {
 *      propA: 1,
 *      propB: 2,
 *      propC: {
 *          subA: 3,
 *          subB: {
 *             testA: 5,
 *             testB: 'Hello'
 *          }
 *      }
 *  }
 *  getPropFromString(jsn,'propC.subB.testB') will return 'Hello';
 */
function getPropFromString(jsn, str, sep = '.') {
    const arr = str.split(sep);
    return arr.reduce((obj, key) =>
        (obj && obj.hasOwnProperty(key)) ? obj[key] : undefined, jsn);
};

/*
    Quick utility to lighten or darken a color (doesn't take color-drifting, etc. into account)
    Usage:
    fadeColor('#061261', 100); // will lighten the color
    fadeColor('#200867'), -100); // will darken the color
*/
function fadeColor(col, amt) {
    const min = Math.min, max = Math.max;
    const num = parseInt(col.replace(/#/g, ''), 16);
    const r = min(255, max((num >> 16) + amt, 0));
    const g = min(255, max((num & 0x0000FF) + amt, 0));
    const b = min(255, max(((num >> 8) & 0x00FF) + amt, 0));
    return '#' + (g | (b << 8) | (r << 16)).toString(16).padStart(6, 0);
}

// this is our global websocket, used to communicate from/to Stream Deck software
// and some info about our plugin, as sent by Stream Deck software
var websocket = null,
    uuid = null,
    actionInfo = {},
    inInfo = {},
    runningApps = [],
    settings = {},
    isQT = navigator.appVersion.includes('QtWebEngine'),
    onchangeevt = 'onchange'; // 'oninput'; // change this, if you want interactive elements act on any change, or while they're modified
