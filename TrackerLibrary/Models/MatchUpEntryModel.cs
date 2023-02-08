using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    public class MatchUpEntryModel
    {
        /// <summary>
        /// The unique identifier for the matchup entry
        /// </summary>
        public int Id { get; set; }

        public int TeamCompetingId { get; set; }
        /// <summary>
        /// Represents one team in matchup.
        /// </summary>
        public TeamModel TeamCompeting { get; set; }
        /// <summary>
        /// Represents the score for this particular team.
        /// </summary>
        public double Score { get; set; }
        /// <summary>
        /// The unique identifier for the matchup (team)
        /// </summary>
        public int ParentMatchupId { get; set; }
        /// <summary>
        /// Represents the matchup that this team came 
        /// from as the winner.
        /// </summary>
        public MatchUpModel ParentMatchUp { get; set; }
    }
}
