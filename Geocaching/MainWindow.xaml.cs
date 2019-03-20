using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
            model.Entity<FoundGeocache>()
                .HasKey(fg => new { fg.PersonId, fg.GeocacheId });

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
                        this.StreetNumber + " | " + this.Longitude + " | " + this.Latitude;
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
            string tmp = this.GeocacheId + " | " + this.Longitude + " | " +
                            this.Latitude + " | " + this.Content + " | " + this.Message;
            return tmp;
        }
    }

    public class FoundGeocache
    {
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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key

        //private const string applicationId = "AlHft3M8psUuZKMImUHduIp_6mnmKRHDIbnRpQr82sfnLC8LS-IZz2vCCF1HTdgi";
        private const string applicationId = "AlHft3M8psUuZKMImUHduIp_6mnmKRHDIbnRpQr82sfnLC8LS-IZz2vCCF1HTdgi";

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Person currentPerson = null;

        private Location gothenburg = new Location(57.719021, 11.991202);

        private AppDbContext database = new AppDbContext();

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //if (applicationId == null)
            //{
            //    MessageBox.Show("Please set the applicationId variable before running this program.");
            //    Environment.Exit(0);
            //}

            CreateMap();
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OnMapLeftClick();
                }
            };

            UpdateMap();

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;

            var hejMenuItem = new MenuItem { Header = "Hej" };
            map.ContextMenu.Items.Add(hejMenuItem);
        }

        private void UpdateMap()
        {
            if (database.Geocache.Where(g => g.Longitude == latestClickLocation.Longitude && g.Latitude == latestClickLocation.Latitude) == null)
            {

            }
            else
            {
                foreach (Person person in database.Person)
                {
                    Location location = new Location { Longitude = person.Longitude, Latitude = person.Latitude };
                    var pin = AddPin(location, person.FirstName + " " + person.LastName, Colors.Blue, 1);

                    pin.MouseDown += (s, a) =>
                    {
                        currentPerson = person;
                        UpdatePin(pin, Colors.Blue, 1);

                        foreach (Pushpin p in layer.Children)
                        {
                            if (p.Background.ToString() == Brushes.Blue.ToString() && p.ToolTip.ToString() != person.FirstName + " " + person.LastName)
                            {
                                UpdatePin(p, Colors.Blue, 0.5);
                            }
                        }
                        // Handle click on person pin here.
                        // Change opacity on the other peoplepins here. But how??
                        // Also change 

                        //MessageBox.Show("You clicked a person");
                        //UpdateMap();
                        // Prevent click from being triggered on map.

                        a.Handled = true;
                    };
                }

                foreach (Geocache g in database.Geocache)
                {
                    Location location = new Location { Longitude = g.Longitude, Latitude = g.Latitude };
                    var pin = AddPin(location, g.Content, Colors.Gray, 1);
                    pin.MouseDown += (s, a) =>
                    {
                        // Handle click on geocache pin here.
                        // If a pin is green, make it red
                        // and also add to the database that the geocache is found. (HARDEST PART EVER?)
                        // vice versa
                        MessageBox.Show("You clicked a geocache");
                        UpdateMap();

                        // Prevent click from being triggered on map.
                        a.Handled = true;
                    };
                }
            }

            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
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
                Latitude = latestClickLocation.Latitude
            };
            database.Add(geocache);
            database.SaveChanges();

            UpdateMap();
        }

        private void OnAddPersonClick(object sender, RoutedEventArgs args)
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
            database.Add(person);
            database.SaveChanges();

            UpdateMap();
        }

        private Pushpin AddPin(Location location, string tooltip, Color color, double opacity)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        private void UpdatePin(Pushpin pin, Color color, double opacity)
        {
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
        }

        // Spara allt i textfilen till databasen.
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

            database.Person.RemoveRange(database.Person);
            database.Geocache.RemoveRange(database.Geocache);
            database.FoundGeocache.RemoveRange(database.FoundGeocache);
            database.SaveChanges();

            string[] lines = File.ReadAllLines(path).ToArray();

            List<List<string>> collection = new List<List<string>>();
            List<string> linesWithObjects = new List<string>();
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


            List<Person> people = new List<Person>();
            Person p;
            List<Geocache> geocaches = new List<Geocache>();
            Geocache geocache;
            Dictionary<string[], Person> pairs = new Dictionary<string[], Person>();

            for (int i = 0; i < collection.Count(); i++)
            {
                // Because [i][0] always contains the person object in our instance.
                string[] values = collection[i][0].Split('|').Select(v => v.Trim()).ToArray();
                p = new Person
                {
                    FirstName = values[0],
                    LastName = values[1],
                    Country = values[2],
                    City = values[3],
                    StreetName = values[4],
                    StreetNumber = byte.Parse(values[5]),
                    Longitude = double.Parse(values[6]),
                    Latitude = double.Parse(values[7]),
                };
                people.Add(p);
                database.Add(p);

                for (int k = 1; k < collection[i].Count(); k++)
                {
                    try
                    {
                        string[] tmp = collection[i][k].Split('|').Select(v => v.Trim()).ToArray();
                        geocache = new Geocache
                        {
                            // Because tmp[0] is the GeocacheId
                            Longitude = double.Parse(tmp[1]),
                            Latitude = double.Parse(tmp[2]),
                            Content = tmp[3],
                            Message = tmp[4],
                            Person = p,
                        };
                        geocaches.Add(geocache);
                        database.Add(geocache);
                    }
                    // When we can't split a line into a geocache object, we know that we have struck the last line.
                    // This means that the current line is an ex. "Found: n, n, n" line.
                    catch
                    {
                        // Do 190km/h until we cant anymore, thus we "know" that we have found found...
                        string[] numbers = collection[i][k].Remove(0, 6).Split(',').Select(v => v.Trim()).ToArray();
                        pairs.Add(numbers, p);
                    }
                }
            }

            List<FoundGeocache> found = new List<FoundGeocache>();

            pairs.Select(pair => pair).ToList()
                .ForEach(entry => entry.Key.Select(k => k).ToList()
                .ForEach(key => 
                database.Add(new FoundGeocache { Person = entry.Value, Geocache = geocaches[(int.Parse(key) - 1)] })
                ));

            // The same as the one above
            //foreach (KeyValuePair<string[], Person> item in pairs)
            //{
            //    item.Key.Select(t => t).ToList()
            //        .ForEach(t => database.Add(new FoundGeocache { Person = item.Value, Geocache = geocaches[(int.Parse(t) - 1)] }));
            //}
            database.SaveChanges();
        }


        // Hämta allt från databasen och spara i textfilen.
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

            Person[] people = database.Person.Select(p => p).OrderByDescending(a => a).ToArray();
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
            lines.RemoveAt(lines.Count()-1);
            File.WriteAllLines(path, lines);
        }
    }
}