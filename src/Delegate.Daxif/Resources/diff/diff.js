$(document).ready(function (){
    $('.Collapsable').click(function () {
        var el = $(this).attr('tag');
        $('a[tag='+el+']').parent().children().toggle();
        $('a[tag='+el+']').toggle();
        $(this).toggleClass('collapsed');
        var currentElement = $(this)
        $('.Collapsable').each(function (){
          if(!$.contains($(this).parent().get(0),currentElement.get(0)) && this !== currentElement.get(0) && $(this).hasClass('collapsed')){
            var otherEl = $(this).attr('tag');
            $('a[tag='+otherEl+']').parent().children().toggle();
            $('a[tag='+otherEl+']').toggle();
            $(this).toggleClass('collapsed');
          }
        });
    });
});
function ajaxhtmlDiff(name, id) {
  $.ajax({
    type: "GET",
    url: "/Daxif/diff/view/id_" + id,
    dataType: "text",
    success: function (data) {
      document.getElementById('viewerContentName').innerHTML = '<a name="Diff-Viewer" class="anchor" href="#Diff-Viewer">Diff Viewer</a> <small>' + name + '<\small>';
      document.getElementById('viewerContent').innerHTML = data;
    },  error: function (xhr) {
      alert(xhr.responseText);
    }
  });
  $('.scrollTop').scrollTop(0);

}
window.onbeforeunload = function() {
  $.ajax({
    type: "GET",
    url: "/exit",
    dataType: "text",
    success: function (data) {}
  });
}