document.addEventListener("DOMContentLoaded", () => {
    const leagueDropdown = document.getElementById("League");
    const homeTeamDropdown = document.getElementById("HomeTeam");
    const awayTeamDropdown = document.getElementById("AwayTeam");

    let allTeams = [];


    leagueDropdown.addEventListener("change", function () {
        const leagueId = this.value;


        homeTeamDropdown.innerHTML = '<option value="">Select Team</option>';
        awayTeamDropdown.innerHTML = '<option value="">Select Team</option>';
        allTeams = [];

        if (leagueId) {
            fetch(`/EditMatch/GetTeamsByLeague?leagueId=${leagueId}`)
                .then(response => response.json())
                .then(data => {
                    allTeams = data;
                    populateDropdowns(data);
                })
                .catch(error => console.error("Error fetching teams:", error));
        }
    });


    function populateDropdowns(teams) {
        teams.forEach(team => {
            const homeOption = document.createElement("option");
            homeOption.value = team.id;
            homeOption.textContent = team.name;
            homeTeamDropdown.appendChild(homeOption);

            const awayOption = document.createElement("option");
            awayOption.value = team.id;
            awayOption.textContent = team.name;
            awayTeamDropdown.appendChild(awayOption);
        });
    }


    homeTeamDropdown.addEventListener("change", function () {
        const selectedHomeTeamId = this.value;
        filterTeams(awayTeamDropdown, selectedHomeTeamId);
    });



    function filterTeams(targetDropdown, selectedTeamId) {

        targetDropdown.innerHTML = '<option value="">Select Team</option>';

        allTeams
            .filter(team => team.id !== parseInt(selectedTeamId))
            .forEach(team => {
                const option = document.createElement("option");
                option.value = team.id;
                option.textContent = team.name;
                targetDropdown.appendChild(option);
            });
    }
});
