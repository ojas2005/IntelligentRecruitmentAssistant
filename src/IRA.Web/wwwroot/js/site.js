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

    // ----- Scroll reveal -----
    var reveals = document.querySelectorAll('.reveal');
    if (reveals.length) {
        if (reduceMotion || !('IntersectionObserver' in window)) {
            reveals.forEach(function (el) { el.classList.add('in'); });
        } else {
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

    // ----- Mobile nav toggle -----
    var nav = document.querySelector('.app-nav');
    var toggle = document.querySelector('.nav-toggle');
    if (nav && toggle) {
        toggle.addEventListener('click', function () { nav.classList.toggle('open'); });
    }
})();
