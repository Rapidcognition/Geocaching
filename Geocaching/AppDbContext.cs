using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;
using Microsoft.EntityFrameworkCore;
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
            model.Entity<Person>()
                .OwnsOne(p => p.GeoCoordinate, gc =>
                    {
                        gc.Property(p => p.Latitude).HasColumnName("Latitude");
                        gc.Property(p => p.Longitude).HasColumnName("Longitude");
                    }
                )
                .OwnsOne(p => p.GeoCoordinate)
                .Ignore(gc => gc.Altitude).Ignore(gc => gc.Course)
                .Ignore(gc => gc.HorizontalAccuracy).Ignore(gc => gc.IsUnknown)
                .Ignore(gc => gc.Speed).Ignore(gc => gc.VerticalAccuracy);


            model.Entity<Geocache>()
                .OwnsOne(g => g.GeoCoordinate, gc =>
                    {
                        gc.Property(p => p.Latitude).HasColumnName("Latitude");
                        gc.Property(p => p.Longitude).HasColumnName("Longitude");
                    }
                )
                .OwnsOne(g => g.GeoCoordinate)
                .Ignore(gc => gc.Altitude).Ignore(gc => gc.Course)
                .Ignore(gc => gc.HorizontalAccuracy).Ignore(gc => gc.IsUnknown)
                .Ignore(gc => gc.Speed).Ignore(gc => gc.VerticalAccuracy);


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

        [Column(TypeName = "nvarchar(50)")]
        public string FirstName { get; set; }
        [Column(TypeName = "nvarchar(50)")]
        public string LastName { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string Country { get; set; }
        [Column(TypeName = "nvarchar(50)")]
        public string City { get; set; }
        [Column(TypeName = "nvarchar(50)")]
        public string StreetName { get; set; }
        public byte StreetNumber { get; set; }

        public GeoCoordinate GeoCoordinate { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        public string ToCsvFormat()
        {
            string tmp = this.FirstName + " | " + this.LastName + " | " +
                        this.Country + " | " + this.City + " | " + this.StreetName + " | " +
                        this.StreetNumber + " | " + GeoCoordinate.Latitude + " | " + GeoCoordinate.Longitude;
            return tmp;
        }

        public string GetTooltipMessage()
        {
            string tooltip = this.FirstName + " " + this.LastName + "\r" + this.StreetName + " " + this.StreetNumber + ", " + this.City;
            return tooltip;
        }
    }

    public class Geocache
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeocacheId { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string Content { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string Message { get; set; }

        public int? PersonId { get; set; }
        public Person Person { get; set; }

        public GeoCoordinate GeoCoordinate { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        public string ToCsvFormat()
        {
            string tmp = this.GeocacheId + " | " + GeoCoordinate.Latitude.ToString() + " | " +
                            GeoCoordinate.Longitude.ToString() + " | " + this.Content + " | " + this.Message;
            return tmp;
        }

        public string GetTooltipMessage()
        {
            string tooltip = this.GeoCoordinate.Latitude + ", " + this.GeoCoordinate.Longitude + "\r" + this.Person.FirstName + " "
                    + this.Person.LastName + " placerade ut denna geocache med " + this.Content + " i. \r \"" + this.Message + "\"";
            return tooltip;
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

        public static string ToCsvFormat(List<FoundGeocache> foundCaches)
        {
            string stringBuilder = "Found: ";
            for (int i = 0; i < foundCaches.Count; i++)
            {
                stringBuilder += foundCaches[i].GeocacheId;
                if (i < foundCaches.Count - 1)
                {
                    stringBuilder += ", ";
                }
            }
            return stringBuilder;
        }
    }
}