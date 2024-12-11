using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Beadando.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExportController : Controller
    {
        private readonly SportsDbContext _context;

        public ExportController(SportsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
            ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
            return View();
        }
        [HttpPost]
        public IActionResult ExportToXml(string filterType, List<int> filterIds)
        {
            if (filterIds == null || !filterIds.Any())
            {
                TempData["ExportErrorMessage"] = "Please select at least one filter!";
                return RedirectToAction("Index");
            }

            IQueryable<Match> matches = _context.Matches;

            if (filterType == "team")
            {
                
                matches = matches.Where(m => filterIds.Contains((int)m.HomeTeamId) || filterIds.Contains((int)m.AwayTeamId));
            }
            else if (filterType == "league")
            {
                
                matches = matches.Where(m => filterIds.Contains((int)m.LeagueId));
            }

            
            var matchData = matches
                .Select(m => new
                {
                    m.Id,
                    HomeTeam = m.HomeTeam.Name,
                    AwayTeam = m.AwayTeam.Name,
                    m.FinalScore,
                    m.HalfTimeScore,
                    m.StartTime,
                    League = m.League.Name,
                    m.Report
                })
                .ToList();

            
            var xml = new XElement("Matches",
                matchData.Select(md => new XElement("Match",
                    new XElement("Id", md.Id),
                    new XElement("HomeTeam", md.HomeTeam),
                    new XElement("AwayTeam", md.AwayTeam),
                    new XElement("FinalScore", md.FinalScore),
                    new XElement("HalfTimeScore", md.HalfTimeScore),
                    new XElement("StartTime", md.StartTime),
                    new XElement("League", md.League),
                    new XElement("Report", md.Report)
                ))
            );

            var xmlString = xml.ToString();
            var fileName = $"Matches_{filterType}_{string.Join("-", filterIds)}.xml";

            return File(Encoding.UTF8.GetBytes(xmlString), "application/xml", fileName);
        }




    }
}
