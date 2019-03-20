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
    }

    public class FoundGeocache
    {
        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }

        [ForeignKey("GeocacheId")]
        public int GeocacheId { get; set; }
        public Geocache Geocache { get; set; }
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

            //// If click is on a person-pin
            //if (database.Person.Where(p => p.Longitude == latestClickLocation.Longitude && p.Latitude == latestClickLocation.Latitude) != null)
            //{
            //    Person person = new Person();
            //    var pin = AddPin(latestClickLocation, person.FirstName + " " + person.LastName, Colors.Blue, 1);
            //    foreach (Person p in database.Person)
            //    {
            //        if (p != person)
            //        {
            //            Location location = new Location { Longitude = p.Longitude, Latitude = p.Latitude };
            //            var pin2 = AddPin(location, p.FirstName + " " + p.LastName, Colors.Blue, 0.5);
            //        }
            //    }
            //    foreach (Geocache g in database.Geocache)
            //    {
            //        Location location = new Location { Longitude = g.Longitude, Latitude = g.Latitude };
            //        if (g.PersonId == person.PersonId)
            //        {
            //            var pin2 = AddPin(location, g.Content, Colors.Black, 1);
            //        }
            //        else if (database.FoundGeocache.Where(fg => fg.PersonId == person.PersonId && fg.GeocacheId == g.GeocacheId) != null)
            //        {
            //            var pin2 = AddPin(location, g.Content, Colors.Green, 1);
            //        }
            //        else
            //        {
            //            var pin2 = AddPin(location, g.Content, Colors.Red, 1);
            //        }
            //    }
            //}
            //// If click is not on pin
            if (database.Geocache.Where(g => g.Longitude == latestClickLocation.Longitude && g.Latitude == latestClickLocation.Latitude) == null)
            {

            }
            else
            {
                foreach (Person p in database.Person)
                {
                    Location location = new Location { Longitude = p.Longitude, Latitude = p.Latitude };
                    var pin = AddPin(location, p.FirstName + " " + p.LastName, Colors.Blue, 1);

                    pin.MouseDown += (s, a) =>
                    {
                    // Handle click on person pin here.
                    MessageBox.Show("You clicked a person");
                        UpdateMap();

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

            string[] lines = File.ReadAllLines(path).ToArray();
            List<Person> people = new List<Person>();
            List<Geocache> geocaches = new List<Geocache>();
            Geocache geocache;
            List<FoundGeocache> found = new List<FoundGeocache>();
            FoundGeocache foundcache;
            Dictionary<string[], Person> pairs = new Dictionary<string[], Person>();


            List<List<string>> collection = new List<List<string>>();
            List<string> liness = new List<string>();
            foreach (var line in lines)
            {
                if(line != "")
                {
                    liness.Add(line);
                    continue;
                }
                else
                {
                    collection.Add(liness);
                    liness = new List<string>();
                }
            }
            collection.Add(liness);

            Person p;
            for (int i = 0; i < collection.Count(); i++)
            {
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
                for (int k = 1; k < collection[i].Count(); k++)
                {
                    try
                    {
                        string[] tmp = collection[i][k].Split('|').Select(v => v.Trim()).ToArray();
                        geocache = new Geocache
                        {
                            Longitude = double.Parse(tmp[1]),
                            Latitude = double.Parse(tmp[2]),
                            Content = tmp[3],
                            Message = tmp[4],
                            Person = p,
                        };
                        geocaches.Add(geocache);
                    }
                    catch(Exception e)
                    {
                        // Insert cheat here
                        string[] numbers = collection[i][k].Remove(0, 6).Split(',').Select(v => v.Trim()).ToArray();
                        pairs.Add(numbers, p);
                    }
                }
            }

            foreach (KeyValuePair<string[], Person> item in pairs)
            {
                string[] tmp = item.Key.Select(t => t).ToArray();

                foreach (var t in tmp)
                {
                    foundcache = new FoundGeocache
                    {
                        Person = item.Value,
                        Geocache = geocaches[(int.Parse(t) - 1)],
                    };
                    found.Add(foundcache);
                    database.Add(foundcache);
                }
            }

            foreach (Person person in people)
            {
                database.Add(person);
            }
            foreach (Geocache cache in geocaches)
            {
                database.Add(cache);
            }

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

            Person[] people = database.Person.Select(p => p).ToArray();
            foreach (Person person in people)
            {
                string stringBuilder = null;
                try
                {
                    stringBuilder += person.FirstName + " | " + person.LastName + " | " +
                        person.Country + " | " + person.City + " | " + person.StreetName + " | " +
                        person.StreetNumber + " | " + person.Longitude + " | " + person.Latitude;
                    lines.Add(stringBuilder);
                    Geocache[] geocaches = database.Geocache.Where(g => g.PersonId == person.PersonId).ToArray();
                    foreach (var item in geocaches)
                    {
                        stringBuilder = null;
                        stringBuilder += item.GeocacheId + " | " + item.Longitude + " | " +
                            item.Latitude + " | " + item.Content + " | " + item.Message;
                        lines.Add(stringBuilder);
                    }
                    FoundGeocache[] foundcaches = database.FoundGeocache.Where(f => f.PersonId == person.PersonId).ToArray();
                    stringBuilder = "Found: ";
                    foreach (var item in foundcaches)
                    {
                        stringBuilder += item.GeocacheId + ", ";
                    }
                    stringBuilder = stringBuilder.Remove(stringBuilder.Length);
                    lines.Add(stringBuilder);
                    lines.Add(Environment.NewLine);
                }
                catch
                {

                }
                lines.RemoveRange(lines.Count() - 1, lines.Count());
            }
        }
    }
}