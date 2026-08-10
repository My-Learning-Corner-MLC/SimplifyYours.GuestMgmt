// The only script in the rendered document.
//
// The frame is sandboxed with allow-scripts but WITHOUT allow-same-origin, so this code cannot
// reach the host page's DOM, cookies, or storage. postMessage is the entire interface, and it
// carries no data: the parent treats a message purely as "the guest clicked RSVP" and reads
// everything it needs from its own authenticated state.
(function () {
  'use strict';

  var TARGET_ORIGIN = document.documentElement.getAttribute('data-parent-origin') || '*';

  function post(message) {
    if (window.parent === window) {
      return;
    }

    window.parent.postMessage(message, TARGET_ORIGIN);
  }

  document.addEventListener('click', function (event) {
    var trigger = event.target && event.target.closest
      ? event.target.closest('.sy-rsvp-trigger')
      : null;

    if (!trigger) {
      return;
    }

    event.preventDefault();
    post({ type: 'sy:rsvp' });
  });

  // The host sizes the frame from this rather than guessing, so a long invitation is never
  // clipped and never gets a nested scrollbar.
  function reportHeight() {
    post({
      type: 'sy:height',
      height: Math.ceil(document.documentElement.scrollHeight)
    });
  }

  if (typeof ResizeObserver === 'function') {
    new ResizeObserver(reportHeight).observe(document.documentElement);
  }

  window.addEventListener('load', reportHeight);
  reportHeight();
})();
