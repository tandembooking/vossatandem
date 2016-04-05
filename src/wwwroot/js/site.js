// Write your Javascript code.
var dh = dh || {};
(function ($) {
    function inithome() {
        $(document).ready(function () {
            $("body").backstretch([
                "/images/bg1.jpg",
                "/images/b3.jpg",
                "/images/b4.jpg",
                "/images/b5.jpg",
                "/images/b6.jpg"
            ], { duration: 4000, fade: 500 });
        });
    }
    dh.inithome = inithome;
}(jQuery));