using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Device.Location;

namespace Geocaching
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            //model.Entity<FoundGeocache>()
            //    .HasKey(fg => new { fg.PersonId, fg.GeocacheId });

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Person)
                .WithMany(p => p.FoundGeocaches)
                .HasForeignKey(fg => fg.PersonId);

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Geocache)
                .WithMany(g => g.FoundGeocaches)
                .HasForeignKey(fg => fg.GeocacheId);
        }
    }

    public class Person
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PersonId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string LastName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string Country { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string City { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string StreetName { get; set; }
        public byte StreetNumber { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        // Helper function to build strings that suit format of the textfile.
        public override string ToString()
        {
            string tmp = this.FirstName + " | " + this.LastName + " | " +
                        this.Country + " | " + this.City + " | " + this.StreetName + " | " +
                        this.StreetNumber + " | " + this.Latitude + " | " + this.Longitude;
            return tmp;
        }
    }

    public class Geocache
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeocacheId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string Content { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string Message { get; set; }

        public int? PersonId { get; set; }
        public Person Person { get; set; }
        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        // Helper function to build strings that suit format of the textfile.
        public override string ToString()
        {
            string tmp = this.GeocacheId + " | " + this.Latitude + " | " +
                            this.Longitude + " | " + this.Content + " | " + this.Message;
            return tmp;
        }
    }

    public class FoundGeocache
    {
        [Key]
        public int FoundGeocacheId { get; set; }

        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }

        [ForeignKey("GeocacheId")]
        public int GeocacheId { get; set; }
        public Geocache Geocache { get; set; }

        public static string BuildFoundString(FoundGeocache[] foundcaches)
        {
            string stringBuilder = "Found: ";
            for (int i = 0; i < foundcaches.Length; i++)
            {
                stringBuilder += foundcaches[i].GeocacheId;
                if (i < foundcaches.Length - 1)
                {
                    stringBuilder += ", ";
                }
            }
            return stringBuilder;
        }
    }

    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "AlHft3M8psUuZKMImUHduIp_6mnmKRHDIbnRpQr82sfnLC8LS-IZz2vCCF1HTdgi";

        private MapLayer layer;

        private Person currentPerson = null;

        private GeoCoordinate gothenburg = new GeoCoordinate { Latitude = 57.719021, Longitude = 11.991202 };

        private GeoCoordinate geo;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private GeoCoordinate latestClickLocation;

        private AppDbContext database = new AppDbContext();

        // For the lock when doing an async operation.
        Object myLock = new Object();

        private Task readToFile;

        public MainWindow()
        {
            latestClickLocation = gothenburg;
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            CreateMap();
        }

        // TODO: Async operations on Read and Write.
        // DONE
        private async void CreateMap()
        {
            try
            {
                layer.Children.Clear();
            }
            catch { }
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = new Location { Latitude = gothenburg.Latitude, Longitude = gothenburg.Longitude };
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation.Latitude = map.ViewportPointToLocation(point).Latitude;
                latestClickLocation.Longitude = map.ViewportPointToLocation(point).Longitude;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    currentPerson = null;
                    foreach (Pushpin pin in layer.Children)
                    {
                        try
                        {
                            Geocache geocache = (Geocache)pin.Tag;
                            UpdatePin(pin, Colors.Gray, 1);
                        }
                        catch
                        {
                            Person person = (Person)pin.Tag;
                            UpdatePin(pin, Colors.Blue, 1);
                        }
                    }
                }
            };

            var people = await Task.Run( () =>
            {
                return database.Person.ToArray();
            });
            foreach (Person person in people)
            {
                geo = new GeoCoordinate();
                geo.Longitude = person.Longitude;
                geo.Latitude = person.Latitude;
                string tooptipp = person.FirstName + " " + person.LastName + "\r" + person.StreetName + " " + person.StreetNumber + ", " + person.City;
                var pin = AddPin(geo, tooptipp, Colors.Blue, 1, person);

                pin.MouseDown += PersonClick;
            }

            var geocaches = await Task.Run(() =>
            {
                return database.Geocache.Include(g => g.Person);
            });
            foreach (Geocache g in geocaches)
            {
                geo = new GeoCoordinate();
                geo.Longitude = g.Longitude;
                geo.Latitude = g.Latitude;
                // Om Click Event exists, then remove. Only click event possible should be ClickGreenButton or ClickRedButton
                string tooltipp = g.Latitude + ", " + g.Longitude + "\r" + g.Person.FirstName + " " + g.Person.LastName + " placerade ut denna geocache med " + g.Content + " i. \r \"" + g.Message + "\"";
                var pin = AddPin(geo, tooltipp, Colors.Gray, 1, g);
                // koordinater, meddelande, innehåll och vilken person som har placerat den.
            }

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        // TODO: Async operations on Read and Write.
        // Bug = "geocache" already has an ID but the database generates it's own id's.
        private async void ClickGreenButton(object sender, MouseButtonEventArgs e)
        {
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;

            FoundGeocache foundGeocache = database.FoundGeocache
                .FirstOrDefault(fg => fg.PersonId == currentPerson.PersonId && fg.GeocacheId == geocache.GeocacheId);

            var task = Task.Run( () =>
            {
                database.Remove(foundGeocache);
                database.SaveChanges();
            });
            await Task.WhenAll(task);

            UpdatePin(pin, Colors.Red, 1);
            pin.MouseDown += ClickRedButton;
            pin.MouseDown -= ClickGreenButton;
            e.Handled = true;
        }

        // TODO: Async operations on Read and Write.
        // Bug = "geocache" already has an ID but the database generates it's own id's.
        private void ClickRedButton(object sender, MouseButtonEventArgs e)
        {
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;

            FoundGeocache foundGeocache = new FoundGeocache
            {
                Person = currentPerson,
                Geocache = geocache,
            };
            var task = Task.Run(() =>
            {
                database.Add(foundGeocache);
                database.SaveChanges();
            });
            Task.WaitAll(task);

            UpdatePin(pin, Colors.Green, 1);
            pin.MouseDown += ClickGreenButton;
            pin.MouseDown -= ClickRedButton;
            e.Handled = true;
        }

        private void Handled(object sender, MouseButtonEventArgs e)
        {
             e.Handled = true;
        }

        // TODO: Async operations on Read and Write.
        // DONE maybe ??
        private void PersonClick(object sender, MouseButtonEventArgs e)
        {
            Geocache[] geocaches = null;
            var geocachez = Task.Run(() =>
            {
                geocaches = database.Geocache.Select(a => a).ToArray();
            });

            Pushpin pin = (Pushpin)sender;
            Person person = (Person)pin.Tag;
            string tooptipp = pin.ToolTip.ToString();
            currentPerson = person;
            UpdatePin(pin, Colors.Blue, 1);

            Task.WaitAll(geocachez);

            foreach (Pushpin p in layer.Children)
            {

                try { p.MouseDown -= ClickGreenButton; }
                catch { }
                try { p.MouseDown -= ClickRedButton; }
                catch { }
                try { p.MouseDown -= Handled; }
                catch { }

                Geocache geocache = geocaches
                    .FirstOrDefault(g => g.Longitude == p.Location.Longitude && g.Latitude == p.Location.Latitude);

                FoundGeocache foundGeocache = null;
                if (geocache != null)
                {
                    foundGeocache = database.FoundGeocache
                        .FirstOrDefault(fg => fg.GeocacheId == geocache.GeocacheId && fg.PersonId == person.PersonId);
                }

                // If the pushpin represents a person, dabble with opacity
                if (geocache == null && p.ToolTip.ToString() != tooptipp)
                {
                    UpdatePin(p, Colors.Blue, 0.5);
                }

                // Otherwise the pushpin is a geocache. In this case the geocache is put there by the current person. So it should become black.
                else if (geocache != null && geocache.PersonId == person.PersonId) // Är default null?
                {
                    UpdatePin(p, Colors.Black, 1);
                    p.MouseDown += Handled;
                }

                // A Geocache found by the current person. Should have clickevent.
                else if (geocache != null && foundGeocache != null)
                {
                    UpdatePin(p, Colors.Green, 1);
                    p.MouseDown += ClickGreenButton;
                }

                // A geocache not found by the current person. Change color, clickevent too.
                else if (geocache != null && foundGeocache == null)
                {
                    UpdatePin(p, Colors.Red, 1);
                    p.MouseDown += ClickRedButton;
                }

                // Prevent click from being triggered on map.
                e.Handled = true;
            }
        }
        
        // TODO: Async operations on Read and Write.
        // DONE
        private async void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            if (currentPerson != null)
            {
                var dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();
                if (dialog.DialogResult == false)
                {
                    return;
                }

                string contents = dialog.GeocacheContents;
                string message = dialog.GeocacheMessage;
                // Add geocache to map and database here.

                // Add to database
                Geocache geocache = new Geocache
                {
                    Content = contents,
                    Message = message,
                    Longitude = latestClickLocation.Longitude,
                    Latitude = latestClickLocation.Latitude,
                    Person = currentPerson,
                };

                await database.AddAsync(geocache);
                await database.SaveChangesAsync();

                GeoCoordinate geo = new GeoCoordinate();
                geo.Longitude = geocache.Longitude;
                geo.Latitude = geocache.Latitude;

                string tooltipp = geocache.Latitude + ", " + geocache.Longitude + "\r" + geocache.Person.FirstName + " " + geocache.Person.LastName + " placerade ut denna geocache med " + geocache.Content + " i. \r \"" + geocache.Message + "\"";

                var pin = AddPin(geo, tooltipp, Colors.Black, 1, geocache);
                pin.MouseDown += Handled;
            }
            else
            {
                MessageBox.Show("Please select a person before creating a geocache.");
            }
        }

        // TODO: Async operations on Read and Write.
        // DONE 
        private async void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string firstName = dialog.PersonFirstName;
            string lastName = dialog.PersonLastName;
            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            byte streetNumber = dialog.AddressStreetNumber;

            // Add to database
            Person person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                City = city,
                Country = country,
                StreetName = streetName,
                StreetNumber = streetNumber,
                Longitude = latestClickLocation.Longitude,
                Latitude = latestClickLocation.Latitude
            };

            await database.AddAsync(person);
            await database.SaveChangesAsync();

            GeoCoordinate geo = new GeoCoordinate();
            geo.Longitude = person.Longitude;
            geo.Latitude = person.Latitude;

            string tooptipp = person.FirstName + " " + person.LastName + "\r" + person.StreetName + " " + person.StreetNumber + ", " + person.City;

            var pin = AddPin(geo, tooptipp, Colors.Blue, 1, person);

            currentPerson = person;

            pin.MouseDown += PersonClick;
        }

        private Pushpin AddPin(GeoCoordinate geo, string tooltip, Color color, double opacity, object o)
        {
            var location = new Location { Longitude = geo.Longitude, Latitude = geo.Latitude };
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
            pin.Location = location;
            pin.Tag = o;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, location);
            return pin;
        }

        private void UpdatePin(Pushpin pin, Color color, double opacity)
        {
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
        }

        // TODO: Async operations on Read and Write.
        // DONE
        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Read the selected file here.

            Task LoadToDatabase = Task.Run(() =>
            {
                database.Person.RemoveRange(database.Person);
                database.Geocache.RemoveRange(database.Geocache);
                database.FoundGeocache.RemoveRange(database.FoundGeocache);
                database.SaveChanges();
            });

            List<List<string>> collection = new List<List<string>>();
            List<string> linesWithObjects = new List<string>();

            List<Person> people = new List<Person>();
            Person person;
            List<Geocache> geocaches = new List<Geocache>();
            Geocache geocache;
            Dictionary<string[], Person> pairs = new Dictionary<string[], Person>();
            Dictionary<int, Geocache> geopairs = new Dictionary<int, Geocache>();

            Task.WaitAll(LoadToDatabase);

            string[] lines = File.ReadAllLines(path).ToArray();

            foreach (var line in lines)
            {
                if(line != "")
                {
                    linesWithObjects.Add(line);
                    continue;
                }
                else
                {
                    collection.Add(linesWithObjects);
                    linesWithObjects = new List<string>();
                }
            }
            collection.Add(linesWithObjects);

            for (int i = 0; i < collection.Count(); i++)
            {
                // Because [i][0] always contains the person object in our instance.
                string[] values = collection[i][0].Split('|').Select(v => v.Trim()).ToArray();

                person = new Person
                {
                    FirstName = values[0],
                    LastName = values[1],
                    Country = values[2],
                    City = values[3],
                    StreetName = values[4],
                    StreetNumber = byte.Parse(values[5]),
                    Latitude = double.Parse(values[6]),
                    Longitude = double.Parse(values[7]),
                };
                people.Add(person);
                database.Add(person);

                for (int k = 1; k < collection[i].Count(); k++)
                {
                    try
                    {
                        string[] tmp = collection[i][k].Split('|').Select(v => v.Trim()).ToArray();
                        geocache = new Geocache
                        {
                            // Because tmp[0] is the GeocacheId
                            Latitude = double.Parse(tmp[1]),
                            Longitude = double.Parse(tmp[2]),
                            Content = tmp[3],
                            Message = tmp[4],
                            Person = person,
                        };
                        geocaches.Add(geocache);
                        geopairs.Add(int.Parse(tmp[0]), geocache);
                        database.Add(person);
                        database.Add(geocache);
                    }
                    // When we can't split a line into a geocache object, we know that we have struck the last line.
                    // This means that the current line is an ex. "Found: n, n, n" line.
                    catch
                    {
                        // Do 190km/h until we cant anymore, thus we "know" that we have found found...
                        string[] numbers = collection[i][k].Remove(0, 6).Split(',').Select(v => v.Trim()).ToArray();
                        if (!numbers.Contains("")) pairs.Add(numbers, person);
                    }
                }
            }

            pairs.Select(pair => pair).ToList()
                .ForEach(entry =>
                    entry.Key.Select(k => k).ToList().ForEach(key =>
                        database.Add(new FoundGeocache { Person = entry.Value, Geocache = geopairs.FirstOrDefault(g => g.Key == (int.Parse(key))).Value })
                ));

            database.SaveChanges();

            // The same as the one above
            //foreach (KeyValuePair<string[], Person> item in pairs)
            //{
            //    item.Key.Select(t => t).ToList()
            //        .ForEach(t => database.Add(new FoundGeocache { Person = item.Value, Geocache = geocaches[(int.Parse(t) - 1)] }));
            //}
            CreateMap();
        }

        // TODO: Fix the async Tasks.
        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Write to the selected file here.

            List<string> lines = new List<string>();

            readToFile = Task.Run(async () =>
            {
                await Task.WhenAll();
                Person[] people = database.Person.Select(p => p).OrderByDescending(a => a).ToArray();
                lock(myLock)
                {
                    foreach (Person person in people)
                    {
                        lines.Add(person.ToString());

                        Geocache[] geocaches = database.Geocache.
                            Where(g => g.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToArray();

                        geocaches.ToList().ForEach(g => lines.Add(g.ToString()));

                        FoundGeocache[] foundcaches = database.FoundGeocache.
                            Where(f => f.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToArray();

                        lines.Add(FoundGeocache.BuildFoundString(foundcaches));
                        lines.Add("");
                    }
                }
                lines.RemoveAt(lines.Count()-1);
            });
            Task.WaitAll(readToFile);
            File.WriteAllLines(path, lines);
            CreateMap();
        }

        private async void SaveAndExit(object sender, RoutedEventArgs args)
        {
            await database.SaveChangesAsync();
            MessageBox.Show("All changes are now saved to the database. See you another time!");
            Environment.Exit(0);
        }
    }
}