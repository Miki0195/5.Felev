function updateSelectedList(selectElementId, listElementId) {
    const selectedOptions = Array.from(document.getElementById(selectElementId).selectedOptions);
    const selectedList = document.getElementById(listElementId);

    selectedList.innerHTML = "";

    selectedOptions.forEach(option => {
        const listItem = document.createElement('li');
        listItem.textContent = option.textContent;
        selectedList.appendChild(listItem);
        });
}

    document.getElementById('teamIds').addEventListener('change', function () {
        updateSelectedList('teamIds', 'selectedTeamsList');
    });

    document.getElementById('leagueIds').addEventListener('change', function () {
        updateSelectedList('leagueIds', 'selectedLeaguesList');
    });

    document.addEventListener("DOMContentLoaded", function () {
        const filterType = document.getElementById('filterType').value;
    const teamsDropdown = document.getElementById('teamsDropdown');
    const leaguesDropdown = document.getElementById('leaguesDropdown');

    if (filterType === "team") {
        teamsDropdown.style.display = "block";
    leaguesDropdown.style.display = "none";
        } else if (filterType === "league") {
        teamsDropdown.style.display = "none";
    leaguesDropdown.style.display = "block";
        }
    });

    document.getElementById('filterType').addEventListener('change', function () {
        const filterType = this.value;
    const teamsDropdown = document.getElementById('teamsDropdown');
    const leaguesDropdown = document.getElementById('leaguesDropdown');
    const teamSelect = document.getElementById('teamIds');
    const leagueSelect = document.getElementById('leagueIds');

    if (filterType === "team") {
        teamsDropdown.style.display = "block";
    leaguesDropdown.style.display = "none";

            Array.from(leagueSelect.options).forEach(option => option.selected = false);
        } else if (filterType === "league") {
        teamsDropdown.style.display = "none";
    leaguesDropdown.style.display = "block";

            Array.from(teamSelect.options).forEach(option => option.selected = false);
        }
    });


    document.querySelector('form').addEventListener('submit', function (event) {
        const filterType = document.getElementById('filterType').value;
    const selectedOptions =
    filterType === "team"
    ? Array.from(document.getElementById('teamIds').selectedOptions)
    : Array.from(document.getElementById('leagueIds').selectedOptions);

    if (selectedOptions.length === 0) {
        alert("Please select at least one option.");
    event.preventDefault();
        }
    });

    document.querySelector('form').addEventListener('submit', function () {
        const filterType = document.getElementById('filterType').value;
    const teamsDropdown = document.getElementById('teamIds');
    const leaguesDropdown = document.getElementById('leagueIds');

    if (filterType === "team") {
        teamsDropdown.name = "filterIds";
    leaguesDropdown.name = "";
        } else if (filterType === "league") {
        teamsDropdown.name = "";
    leaguesDropdown.name = "filterIds";
        }
    });

    function hideErrorMessageOnSelection() {
        const filterType = document.getElementById('filterType').value;
    const errorMessage = document.getElementById('error-message');
    const selectedOptions =
    filterType === "team"
    ? Array.from(document.getElementById('teamIds').selectedOptions)
    : Array.from(document.getElementById('leagueIds').selectedOptions);

        if (selectedOptions.length > 0 && errorMessage) {
        errorMessage.style.display = "none";
        }
    }

    document.getElementById('teamIds').addEventListener('change', hideErrorMessageOnSelection);
    document.getElementById('leagueIds').addEventListener('change', hideErrorMessageOnSelection);

    document.getElementById('filterType').addEventListener('change', function () {
        const errorMessage = document.getElementById('error-message');
    if (errorMessage) {
        errorMessage.style.display = "none"; 
        }
    });
