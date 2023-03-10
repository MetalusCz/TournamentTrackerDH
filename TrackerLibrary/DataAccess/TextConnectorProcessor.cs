using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess.TextHelpers
{
    public static class TextConnectorProcessor
    {
        public static string FullFilePath(this string fileName)
        {
            return $"{ConfigurationManager.AppSettings["filePath"]}\\{fileName}";   // cesta ke slozce v app.configu appSettings TAG !!!
        }

        public static List<string> LoadFile(this string file)
        {
            if (!File.Exists(file)) //pokud neexistuje tak proved, jinak nedelej nic!!!, tim skoci na dalsi return
            {
                return new List<string>();
            }
            return File.ReadAllLines(file).ToList();
        }
        public static List<PrizeModel> ConvertToPrizeModels(this List<string> lines) 
        {
            List<PrizeModel> output = new List<PrizeModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');  //generuje pole, rozplituje carkou na jednotlive stringy
                PrizeModel p = new PrizeModel();
                p.Id = int.Parse(cols[0]);
                p.PlaceNumber = int.Parse(cols[1]);
                p.PlaceName = cols[2];
                p.PrizeAmount = decimal.Parse(cols[3]);
                p.PrizePercentage = double.Parse(cols[4]);

                output.Add(p);
            }
            return output;
        }
        public static List<PersonModel> ConvertToPersonModels(this List<string> lines)
        {
            List<PersonModel> output = new List<PersonModel>();
            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                PersonModel p = new PersonModel();
                p.Id = int.Parse(cols[0]);
                p.FirstName = cols[1];
                p.LastName = cols[2];
                p.EmailAddress = cols[3];
                p.CellPhoneNumber = cols[4];
                output.Add(p);

            }
            return output;
        }
        public static List<TeamModel> ConvertToTeamModels(this List<string> lines)
        {
            //id, team name, list of ids separated by the pipe
            //3, Tim's team, 1|3|5

            List<TeamModel> output = new List<TeamModel>();
            List<PersonModel> people = GlobalConfig.PeopleFile.FullFilePath().LoadFile().ConvertToPersonModels();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                TeamModel t = new TeamModel();
                t.Id = int.Parse(cols[0]);
                t.TeamName = cols[1];

                string[] personIds = cols[2].Split('|');

                foreach (string id in personIds)
                {
                    t.TeamMembers.Add(people.Where(x => x.Id == int.Parse(id)).First());
                }
                output.Add(t);
            }
            return output;
        }

        public static List<TournamentModel> ConvertToTournamentModels(this List<string> lines)
        {
            //id=0
            //TournamentName=1
            //EntryFee =2
            //EnteredTeams =3
            //Prizes =4
            //Rounds=5
            //id,TournamentName,EntryFee, (id|id|id - EnteredTeams), (id|id|id - Prizes), (Rounds - id^id^id|id^id^id|id^id^id|)
            List<TournamentModel> output = new List<TournamentModel>();
            List<TeamModel> teams = GlobalConfig.TeamFile.FullFilePath().LoadFile().ConvertToTeamModels();
            List<PrizeModel> prizes = GlobalConfig.PrizesFile.FullFilePath().LoadFile().ConvertToPrizeModels();
            
            List<MatchUpModel> matchups = GlobalConfig
                .MatchUpFile
                .FullFilePath()
                .LoadFile()
                .ConvertToMatchupModels();
            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                TournamentModel tm = new TournamentModel();
                tm.Id = int.Parse(cols[0]);
                tm.TournamentName = cols[1];
                tm.EntryFee = decimal.Parse(cols[2]);

                string[] teamIds = cols[3].Split('|');
                foreach (string id in teamIds)
                {
                    // t.TeamMembers.Add(people.Where(x => x.Id == int.Parse(id)).First());
                    tm.EnteredTeams.Add(teams.Where(x => x.Id == int.Parse(id)).First());
                }
                string[] prizeIds = cols[4].Split('|');

                foreach (string id in prizeIds)
                {
                    tm.Prizes.Add(prizes.Where(x => x.Id == int.Parse(id)).First());
                }

                // Capture rounds information

                string[] rounds = cols[5].Split('|');
                List<MatchUpModel> ms = new List<MatchUpModel>();
                foreach (string round in rounds)
                {
                    string[] msText = round.Split('^');

                    foreach (string matchupModelTextId in msText)
                    {
                        ms.Add(matchups.Where(x => x.Id == int.Parse(matchupModelTextId)).First());
                    }
                    tm.Rounds.Add(ms);
                }

                output.Add(tm);
            }
            return output;
        }

        public static void SaveToPrizeFile(this List<PrizeModel> models) //Extension method. pridej funkci do List<PrizeModel>
        {
            List<string> lines = new List<string>();
            foreach (PrizeModel p in models)
            {
                lines.Add($"{p.Id},{p.PlaceNumber},{p.PlaceName},{p.PrizeAmount},{p.PrizePercentage}");
            }

            File.WriteAllLines(GlobalConfig.PrizesFile.FullFilePath(), lines); //metoda tridy file, zapise pole retezcu do souboru a pote soubor uzavre
        }
        public static void SaveToPeopleFile(this List<PersonModel> models)
        {
            List<string> lines = new List<string>();
            foreach (PersonModel p in models)
            {
                lines.Add($"{p.Id},{p.FirstName},{p.LastName},{p.EmailAddress},{p.CellPhoneNumber}");
            }

            File.WriteAllLines(GlobalConfig.PeopleFile.FullFilePath(), lines);
        }

        public static void SaveToTeamFile(this List<TeamModel> models)
        {
            List<string> lines = new List<string>();

            foreach (TeamModel t in models)
            {
                lines.Add($"{t.Id},{t.TeamName},{ConvertPeopleListToString(t.TeamMembers)}");
            }

            File.WriteAllLines(GlobalConfig.TeamFile.FullFilePath(), lines);
        }

        private static List<MatchUpEntryModel> ConvertStringToMachupEntryModel(string input) //pokud neni ok Lesson 20
        { 
            string[] ids = input.Split('|');
            List<MatchUpEntryModel> output = new List<MatchUpEntryModel>();
            List<MatchUpEntryModel> entries = GlobalConfig.MatchUpEntryFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();

            foreach (string id in ids)
            {
                output.Add(entries.Where(x => x.Id == int.Parse(id)).First());
            }
            return output;
        }

        private static TeamModel LookupTeamById(int id)
        {
            List<TeamModel> teams = GlobalConfig.TeamFile.FullFilePath().LoadFile().ConvertToTeamModels();

            return teams.Where(x => x.Id == id).First();
        }

        private static MatchUpModel LookupMatchupById(int id)
        {
            List<MatchUpModel> matchups = GlobalConfig.MatchUpFile.FullFilePath().LoadFile().ConvertToMatchupModels();

            return matchups.Where(x => x.Id == id).First();
        }

        public static List<MatchUpModel> ConvertToMatchupModels(this List<string> lines)
        {
            //id=0, entries =1 (pipe delimted by id), winner=2, matchupRound=3
            List<MatchUpModel> output = new List<MatchUpModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                MatchUpModel m = new MatchUpModel();
                m.Id = int.Parse(cols[0]);
                m.Entries = ConvertStringToMachupEntryModel(cols[1]);
                if (cols[2].Length == 0)
                {
                    m.Winner = null;
                }
                else
                {
                    m.Winner = LookupTeamById(int.Parse(cols[2]));  //convert id to teammodel
                }
                m.MatchUpRound = int.Parse(cols[3]);
                output.Add(m);
            }
            return output;
        }

        public static void SaveRoundsToFile(this TournamentModel model)
        {
            // Loop through each round
            // Loop throug each matchup
            // Get the id for the new matchup and save the record
            // Loop through each Entry, get the id, and save it

            foreach (List<MatchUpModel> round in model.Rounds)
            {
                foreach (MatchUpModel matchup in round)
                {
                    //Load all the machups from file
                    //Get the top id and add one
                    //Store the id
                    //Save the Matchup record
                    matchup.SaveMatchupToFile();
                }
            }
        }

        public static List<MatchUpEntryModel> ConvertToMatchupEntryModels(this List<string> lines)
        {
            //id=0, TeamCompeting=1, Score=2, ParentMatchup=3
            List<MatchUpEntryModel> output = new List<MatchUpEntryModel>();
            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                MatchUpEntryModel me = new MatchUpEntryModel();
                me.Id = int.Parse(cols[0]);
                if (cols[1].Length == 0)
                {
                    me.TeamCompeting = null;
                }
                else
                {
                    me.TeamCompeting = LookupTeamById(int.Parse(cols[1]));
                }

                me.Score = double.Parse(cols[2]);
                int parentId = 0;
                if (int.TryParse(cols[3], out parentId))
                {
                    me.ParentMatchUp = LookupMatchupById(parentId);
                }
                else
                {
                    me.ParentMatchUp = null;
                }
                output.Add(me);
            }
            return output;
        }

        public static void SaveMatchupToFile(this MatchUpModel matchup)
        {
            List<MatchUpModel> matchups = GlobalConfig.MatchUpFile
                .FullFilePath()
                .LoadFile()
                .ConvertToMatchupModels();

            int currentId = 1;

            if (matchups.Count > 0)
            {
                currentId = matchups.OrderByDescending(x => x.Id).First().Id + 1; //lo ordena por orden inverso de IDs y le añade 1
            }
            matchup.Id = currentId;

            matchups.Add(matchup);

            //save to file

            List<string> lines = new List<string>();

            //id=0, entries =1 (pipe delimted by id), winner=2, matchupRound=3
            foreach (MatchUpModel m in matchups)
            {
                string winner = "";
                if (m.Winner != null)
                {
                    winner = m.Winner.Id.ToString();
                }
                lines.Add($"{m.Id},{""},{winner},{m.MatchUpRound} ");
            }
            File.WriteAllLines(GlobalConfig.MatchUpFile.FullFilePath(), lines);

            foreach (MatchUpEntryModel entry in matchup.Entries)
            {
                entry.SaveEntryToFile();
            }
            //save to file

            lines = new List<string>();

            //id=0, entries =1 (pipe delimted by id), winner=2, matchupRound=3
            foreach (MatchUpModel m in matchups)
            {
                string winner = "";
                if (m.Winner != null)
                {
                    winner = m.Winner.Id.ToString();
                }
                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries)},{winner},{m.MatchUpRound} ");
            }
            File.WriteAllLines(GlobalConfig.MatchUpFile.FullFilePath(), lines);

        }

        public static void SaveEntryToFile(this MatchUpEntryModel entry)
        {
            List<MatchUpEntryModel> entries = GlobalConfig.MatchUpEntryFile.
                FullFilePath().
                LoadFile().
                ConvertToMatchupEntryModels();

            int currentId = 1;

            if (entries.Count > 0)
            {
                currentId = entries.OrderByDescending(x => x.Id).First().Id + 1;
            }
            entry.Id = currentId;
            entries.Add(entry);

            //save to file

            List<string> lines = new List<string>();

            //id=0, TeamCompeting=1, Score=2, ParentMatchup=3
            foreach (MatchUpEntryModel e in entries)
            {
                string parent = "";
                if (e.ParentMatchUp != null)
                {
                    parent = e.ParentMatchUp.Id.ToString();
                }
                string teamCompeting = "";
                if (e.TeamCompeting != null)
                {
                    teamCompeting = e.TeamCompeting.Id.ToString();
                }
                lines.Add($"{e.Id},{teamCompeting},{e.Score},{parent}");
            }
            File.WriteAllLines(GlobalConfig.MatchUpEntryFile.FullFilePath(), lines);

        }
        public static void UpdateMatchupToFile(this MatchUpModel matchup)
        {
            List<MatchUpModel> matchups = GlobalConfig.MatchUpFile.FullFilePath().LoadFile().ConvertToMatchupModels();

            MatchUpModel oldMatchup = new MatchUpModel();

            foreach (MatchUpModel m in matchups)
            {
                if (m.Id == matchup.Id)
                {
                    oldMatchup = m;
                }
            }

            matchups.Remove(oldMatchup);

            matchups.Add(matchup);

            foreach (MatchUpEntryModel entry in matchup.Entries)
            {
                entry.UpdateEntryToFile();
            }

            List<string> lines = new List<string>();

            foreach (MatchUpModel m in matchups)
            {
                string winner = "";
                if (m.Winner != null)
                {
                    winner = m.Winner.Id.ToString();
                }
                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries)},{winner},{m.MatchUpRound}");
            }

            File.WriteAllLines(GlobalConfig.MatchUpFile.FullFilePath(), lines);
        }
        public static void SaveToTournamentFile(this List<TournamentModel> models)
        {
            List<string> lines = new List<string>();

            foreach (TournamentModel tm in models)
            {
                lines.Add($@"{tm.Id},
                            {tm.TournamentName},
                            {tm.EntryFee},
                            {ConvertTeamListToString(tm.EnteredTeams)},
                            {ConvertPrizeListToString(tm.Prizes)},
                            {ConvertRoundListToString(tm.Rounds)}");
            }
            File.WriteAllLines(GlobalConfig.TournamentFile.FullFilePath(), lines);
        }

        private static string ConvertTeamListToString(List<TeamModel> teams)
        {
            string output = "";

            if (teams.Count == 0)
            {
                return "";
            }

            foreach (TeamModel t in teams)
            {
                output += $"{t.Id}|";

            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }
        public static void UpdateEntryToFile(this MatchUpEntryModel entry)
        {
            List<MatchUpEntryModel> entries = GlobalConfig.MatchUpEntryFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();
            MatchUpEntryModel oldEntry = new MatchUpEntryModel();

            foreach (MatchUpEntryModel e in entries)
            {
                if (e.Id == entry.Id)
                {
                    oldEntry = e;
                }
            }

            entries.Remove(oldEntry);

            entries.Add(entry);

            List<string> lines = new List<string>();

            foreach (MatchUpEntryModel e in entries)
            {
                string parent = "";
                if (e.ParentMatchUp != null)
                {
                    parent = e.ParentMatchUp.Id.ToString();
                }

                string teamCompeting = "";
                if (e.TeamCompeting != null)
                {
                    teamCompeting = e.TeamCompeting.Id.ToString();
                }

                lines.Add($"{e.Id},{teamCompeting},{e.Score},{parent}");
            }

            File.WriteAllLines(GlobalConfig.MatchUpEntryFile.FullFilePath(), lines);
        }
        private static string ConvertMatchupEntryListToString(List<MatchUpEntryModel> entries)
        {
            string output = "";

            if (entries.Count == 0)
            {
                return "";
            }

            foreach (MatchUpEntryModel e in entries)
            {
                output += $"{e.Id}|";

            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertPrizeListToString(List<PrizeModel> prizes)
        {
            string output = "";

            if (prizes.Count == 0)
            {
                return "";
            }

            foreach (PrizeModel p in prizes)
            {
                output += $"{p.Id}|";

            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertRoundListToString(List<List<MatchUpModel>> rounds)
        {
            // (Rounds - id^id^id|id^id^id|id^id^id|)
            string output = "";

            if (rounds.Count == 0)
            {
                return "";
            }

            foreach (List<MatchUpModel> r in rounds)
            {
                output += $"{ConvertMatchupListToString(r)}|";
            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertMatchupListToString(List<MatchUpModel> macthups)
        {
            string output = "";

            if (macthups.Count == 0)
            {
                return "";
            }

            foreach (MatchUpModel m in macthups)
            {
                output += $"{m.Id}^";

            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertPeopleListToString(List<PersonModel> people)
        {
            string output = "";

            if (people.Count == 0)
            {
                return "";
            }
            //2|5|
            foreach (PersonModel p in people)
            {
                output += $"{p.Id}|";

            }
            output = output.Substring(0, output.Length - 1);

            return output;
        }
    }
}
