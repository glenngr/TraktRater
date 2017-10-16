using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;
using TraktRater.Sites.API.iCheckMovies;
using TraktRater.TraktAPI.DataStructures;
using TraktRater.UI;

namespace TraktRater.Sites
{
    public class GenericFileImport : IRateSite
    {
        private string filename;
        private bool importCancelled;
        private readonly CsvConfiguration csvConfiguration = new CsvConfiguration()
        {
            HasHeaderRecord = true,
            IsHeaderCaseSensitive = false,
            Delimiter = ";"
        };

        public GenericFileImport(string csvFile)
        {
            filename = csvFile;
            Enabled = File.Exists(csvFile);
        }

        public string Name => "GenericFileImport";
        public bool Enabled { get; set; }
        public void ImportRatings()
        {
            if (importCancelled)
            {
                return;
            }

            var movieList = ParseGenericCsv();

            if (movieList.Any())
            {
                AddMoviesToWatchlist(movieList);
            }
        }

        private void AddMoviesToWatchlist(List<ImdbIdItem> movieList)
        {
            UIUtils.UpdateStatus("Updating Trakt watchlist with movies from Generic Csv file.");
            var watchlistToSync = new TraktMovieSync()
            {
                Movies = movieList.Select(icm => icm.ToTraktMovie()).ToList()
            };

            var addToWatchlistResponse = TraktAPI.TraktAPI.AddMoviesToWatchlist(watchlistToSync);
            HandleResponse(addToWatchlistResponse);
        }

        public void Cancel()
        {
            importCancelled = true;
        }

        private static void HandleResponse(TraktSyncResponse addToWatchlistResponse)
        {
            if (addToWatchlistResponse == null)
            {
                UIUtils.UpdateStatus("Error importing ICheckMovies list to trakt.tv", true);
                Thread.Sleep(2000);
            }
            else if (addToWatchlistResponse.NotFound.Movies.Count > 0)
            {
                UIUtils.UpdateStatus("Unable to process {0} movies as they're not found on trakt.tv!",
                    addToWatchlistResponse.NotFound.Movies.Count);
                Thread.Sleep(1000);
            }
        }

        private List<ImdbIdItem> ParseGenericCsv()
        {
            UIUtils.UpdateStatus("Parsing Generic CSV file");
            var textReader = File.OpenText(filename);

            var csv = new CsvReader(textReader, csvConfiguration);
            return csv.GetRecords<ImdbIdItem>().ToList();
        }

        private class ImdbIdItem
        {
            public string ImdbId { get; set; }

            public TraktMovie ToTraktMovie()
            {
                return new TraktMovie()
                {
                    Ids = new TraktMovieId() { ImdbId = ImdbId}
                };
            }
        }
    }
}
