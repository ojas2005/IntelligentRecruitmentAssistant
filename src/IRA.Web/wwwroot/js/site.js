// Intelligent Recruitment Assistant — lightweight vanilla interactions.
// No frameworks: pointer-driven 3D tilt, scroll reveal, mobile nav toggle.
(function () {
    'use strict';

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Enable JS-only entrance animations (content stays visible if this never runs).
    document.body.classList.add('has-js');

    // ----- 3D tilt on [data-tilt] cards -----
    if (!reduceMotion && window.matchMedia('(pointer: fine)').matches) {
        var MAX = 7; // degrees
        document.querySelectorAll('[data-tilt]').forEach(function (el) {
            el.addEventListener('pointermove', function (e) {
                var r = el.getBoundingClientRect();
                var px = (e.clientX - r.left) / r.width - 0.5;
                var py = (e.clientY - r.top) / r.height - 0.5;
                el.style.setProperty('--ry', (px * MAX).toFixed(2) + 'deg');
                el.style.setProperty('--rx', (-py * MAX).toFixed(2) + 'deg');
            });
            el.addEventListener('pointerleave', function () {
                el.style.setProperty('--ry', '0deg');
                el.style.setProperty('--rx', '0deg');
            });
        });
    }

    // ----- Scroll reveal (with per-row stagger) -----
    var reveals = document.querySelectorAll('.reveal');
    if (reveals.length) {
        if (reduceMotion || !('IntersectionObserver' in window)) {
            reveals.forEach(function (el) { el.classList.add('in'); });
        } else {
            // Stagger cards that share a Bootstrap .row so they cascade in, not all at once.
            reveals.forEach(function (el) {
                var row = el.closest('.row');
                var idx = 0;
                if (row) {
                    var group = row.querySelectorAll('.reveal');
                    idx = Array.prototype.indexOf.call(group, el);
                }
                el.style.transitionDelay = Math.min(idx, 8) * 70 + 'ms';
            });

            var io = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('in');
                        io.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.12 });
            reveals.forEach(function (el) { io.observe(el); });
        }
    }

    // ----- Mobile nav toggle (animated via .open class + CSS transitions) -----
    var nav = document.querySelector('.app-nav');
    var toggle = document.querySelector('.nav-toggle');
    if (nav && toggle) {
        toggle.addEventListener('click', function () {
            var open = nav.classList.toggle('open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    }
})();
