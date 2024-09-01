﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReHUD.Models.LapData;

#nullable disable

namespace ReHUD.Migrations
{
    [DbContext(typeof(LapDataContext))]
    partial class LapDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.20");

            modelBuilder.Entity("ReHUD.Models.LapData.FuelUsage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DataId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PendingRemoval")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("DataId")
                        .IsUnique();

                    b.ToTable("FuelUsages");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapContext", b =>
                {
                    b.Property<int>("TrackLayoutId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CarId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassPerformanceIndex")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("BestLapId")
                        .HasColumnType("INTEGER");

                    b.HasKey("TrackLayoutId", "CarId", "ClassPerformanceIndex");

                    b.HasIndex("BestLapId");

                    b.HasIndex("TrackLayoutId", "CarId", "ClassPerformanceIndex");

                    b.ToTable("LapsContext");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CarId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassPerformanceIndex")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("TrackLayoutId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Valid")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TrackLayoutId", "CarId", "ClassPerformanceIndex");

                    b.ToTable("LapsData");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapTime", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DataId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PendingRemoval")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("DataId")
                        .IsUnique();

                    b.ToTable("LapTimes");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.Telemetry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DataId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PendingRemoval")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DataId")
                        .IsUnique();

                    b.ToTable("BestLaps");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.TireWear", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DataId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PendingRemoval")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DataId")
                        .IsUnique();

                    b.ToTable("TireWears");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.FuelUsage", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapData", "Data")
                        .WithOne("FuelUsage")
                        .HasForeignKey("ReHUD.Models.LapData.FuelUsage", "DataId");

                    b.Navigation("Data");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapContext", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapData", "BestLap")
                        .WithMany()
                        .HasForeignKey("BestLapId");

                    b.Navigation("BestLap");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapData", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapContext", "Context")
                        .WithMany("Laps")
                        .HasForeignKey("TrackLayoutId", "CarId", "ClassPerformanceIndex")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Context");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapTime", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapData", "Data")
                        .WithOne("LapTime")
                        .HasForeignKey("ReHUD.Models.LapData.LapTime", "DataId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Data");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.Telemetry", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapData", "Data")
                        .WithOne("Telemetry")
                        .HasForeignKey("ReHUD.Models.LapData.Telemetry", "DataId");

                    b.OwnsOne("ReHUD.Models.LapData.TelemetryObj", "Value", b1 =>
                        {
                            b1.Property<int>("TelemetryId")
                                .HasColumnType("INTEGER");

                            b1.Property<double>("PointsPerMeter")
                                .HasColumnType("REAL");

                            b1.HasKey("TelemetryId");

                            b1.ToTable("BestLaps");

                            b1.WithOwner()
                                .HasForeignKey("TelemetryId");
                        });

                    b.Navigation("Data");

                    b.Navigation("Value")
                        .IsRequired();
                });

            modelBuilder.Entity("ReHUD.Models.LapData.TireWear", b =>
                {
                    b.HasOne("ReHUD.Models.LapData.LapData", "Data")
                        .WithOne("TireWear")
                        .HasForeignKey("ReHUD.Models.LapData.TireWear", "DataId");

                    b.OwnsOne("ReHUD.Models.LapData.TireWearObj", "Value", b1 =>
                        {
                            b1.Property<int>("TireWearId")
                                .HasColumnType("INTEGER");

                            b1.Property<double>("FrontLeft")
                                .HasColumnType("REAL");

                            b1.Property<double>("FrontRight")
                                .HasColumnType("REAL");

                            b1.Property<double>("RearLeft")
                                .HasColumnType("REAL");

                            b1.Property<double>("RearRight")
                                .HasColumnType("REAL");

                            b1.HasKey("TireWearId");

                            b1.ToTable("TireWears");

                            b1.WithOwner()
                                .HasForeignKey("TireWearId");
                        });

                    b.Navigation("Data");

                    b.Navigation("Value")
                        .IsRequired();
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapContext", b =>
                {
                    b.Navigation("Laps");
                });

            modelBuilder.Entity("ReHUD.Models.LapData.LapData", b =>
                {
                    b.Navigation("FuelUsage");

                    b.Navigation("LapTime")
                        .IsRequired();

                    b.Navigation("Telemetry");

                    b.Navigation("TireWear");
                });
#pragma warning restore 612, 618
        }
    }
}
