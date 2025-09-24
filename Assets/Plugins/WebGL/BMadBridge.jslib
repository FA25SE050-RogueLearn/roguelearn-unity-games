mergeInto(LibraryManager.library, {
  BMad_SendInit: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-init', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-init', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendInit error', e); }
  },
  BMad_SendQuestion: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-question', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-question', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendQuestion error', e); }
  },
  BMad_SendProgress: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-progress', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-progress', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendProgress error', e); }
  },
  BMad_SendAnswer: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-answer', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-answer', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendAnswer error', e); }
  },
  BMad_SendComplete: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-complete', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-complete', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendComplete error', e); }
  },
  BMad_SendError: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'unity-error', payload: msg }, '*');
      } else {
        window.dispatchEvent(new MessageEvent('message', { data: { type: 'unity-error', payload: msg } }));
      }
    } catch (e) { console.error('BMad_SendError error', e); }
  }
});