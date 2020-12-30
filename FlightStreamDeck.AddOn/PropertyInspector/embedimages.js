// This script is used in pages that has image file input with embed button
function sendToPropertyInspector(payload) {
    switch (payload.action) {
        case "refresh":
            settings = payload.settings;
            loadSettings(settings);
            break;
    }
}

function handleEmbed(_this) {
    var fileKey = _this.dataset.for;
    if (settings[fileKey] && !settings[fileKey + '_base64']) {
        if (confirm('Do you want to embed current image?\n\nClick OK to embed current image or Cancel to choose another image to embed.')) {
            sendValueToPlugin({
                convertToEmbed: fileKey
            });
            return;
        }
    }
    IsEmbedding.value = "true";

    document.getElementById(fileKey).click();
}

function handleLink(_this) {
    var fileKey = _this.dataset.for;
    if (settings[fileKey] && settings[fileKey + '_base64']) {
        if (confirm('Do you want to save current image and link to that version?\n\nnClick OK to save and link current image or Cancel to choose another image to link.')) {
            sendValueToPlugin({
                convertToLink: fileKey
            });
            return;
        }
    }
    IsEmbedding.value = "false";

    document.getElementById(fileKey).click();
}

function handleItemChanged(control, value) {
    const hasValue = !!value.value;
    if (value.key === 'IsEmbedding') {
        return;
    }

    const base64Key = value.key + "_base64";

    //alert(`${value.key} '${value.value}' '${IsEmbedding.value}' ${!!settings[base64Key]}`)

    if (hasValue || !IsEmbedding.value) {
        // Prevent changing settings if user press cancel while selecting file
        settings[value.key] = value.value;
    }

    if (IsEmbedding.value === "true") {
        // Embed flag is on
        // => The selected file path is used for embedding

        if (value.value) {
            readFile(value.key, value.value);

            // Save and send to plugin is done in readfile callback to prevent race condition
            return;
        }
    } else if (IsEmbedding.value === "false" && value.value && settings[base64Key]) {
        // Embed flag is off but there is base64 data in setting
        // => Switch embed to link
        settings[base64Key] = null;
    } else if (!IsEmbedding.value && settings[base64Key]) {
        // Clear
        settings[base64Key] = null;
    }

    if (!!IsEmbedding.value) {
        // Refresh the label
        setFileLabel(control.closest('.sdpi-item'), settings[value.key], !!settings[base64Key]);
    }

    refresh();
}

function refresh() {
    // Clear temp flag
    IsEmbedding.value = "";

    sendValueToPlugin(settings);
    setSettings(settings);
}

async function readFile(key, file) {
    var rawFile = new XMLHttpRequest();
    rawFile.responseType = 'arraybuffer';
    rawFile.open("GET", file, true);
    rawFile.onreadystatechange = function () {
        if (rawFile.readyState === 4) {
            if (rawFile.status === 200 || rawFile.status == 0) {
                // Create a binary string from the returned data, then encode it as a data URL.
                var uInt8Array = new Uint8Array(this.response);
                var i = uInt8Array.length;
                var binaryString = new Array(i);
                while (i--) {
                    binaryString[i] = String.fromCharCode(uInt8Array[i]);
                }
                var data = binaryString.join('');

                var base64 = window.btoa(data);
                settings[key + '_base64'] = base64;
                //settings[key + '_base64'] = rawFile.response;

                refresh();
            }
        }
    }
    rawFile.send(null);
}