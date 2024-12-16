function showDeleteModal(matchId) {
    document.getElementById('matchId').value = matchId;
    var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    deleteModal.show();
}
