$(document).ready(function(){

    $("#sidebar-toggle").on("click", function(e){
        e.preventDefault();

        $("#sidebar").toggleClass("hidden");
    });

});