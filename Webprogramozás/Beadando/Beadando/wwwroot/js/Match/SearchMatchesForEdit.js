function showEditModal(matchId) {
    $.get('/EditMatch/EditMatch', { id: matchId }, function (data) {
        $('#editMatchModalContainer').html(data);
        var modal = new bootstrap.Modal(document.getElementById('editMatchModal'));
        modal.show();


        $('#editMatchModalContainer').find('script').each(function () {
            eval($(this).text());
        });
    });
}
