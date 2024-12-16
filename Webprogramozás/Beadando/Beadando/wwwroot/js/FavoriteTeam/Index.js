document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('leagueSelect').addEventListener('change', function () {
        const leagueId = this.value;

        if (!leagueId) {
            document.getElementById('teamsContainer').innerHTML = "";
            return;
        }

        fetch(`/FavoriteTeam/TeamsByLeague?leagueId=${leagueId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.text();
            })
            .then(html => {
                document.getElementById('teamsContainer').innerHTML = html;
            })
            .catch(error => console.error("Error fetching teams:", error));
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const successMessage = document.querySelector('.favorite-team-success-message');
    if (successMessage) {
        setTimeout(() => {
            successMessage.style.transition = 'opacity 0.5s ease-out';
            successMessage.style.opacity = '0';
            setTimeout(() => successMessage.remove(), 500);
        }, 2000);
    }
});