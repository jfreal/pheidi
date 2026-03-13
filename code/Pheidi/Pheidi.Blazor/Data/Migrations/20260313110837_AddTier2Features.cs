using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pheidi.Blazor.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTier2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentWeeklyMileage",
                table: "UserProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "UserProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunWalkRunMinutes",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunWalkWalkMinutes",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunningExperienceMonths",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransitionTimePreset",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VolumeMode",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "TrainingPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ActualIntensityZone",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionPercent",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRunWalk",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Modifier",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaceZone_RpeDescription",
                table: "ScheduledWorkouts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaceZone_RpeMax",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaceZone_RpeMin",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaceZone_Zone",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadinessScore",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunMinutes",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WalkMinutes",
                table: "ScheduledWorkouts",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentWeeklyMileage",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RunWalkRunMinutes",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RunWalkWalkMinutes",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RunningExperienceMonths",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TransitionTimePreset",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VolumeMode",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "ActualIntensityZone",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "CompletionPercent",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "IsRunWalk",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "Modifier",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "PaceZone_RpeDescription",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "PaceZone_RpeMax",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "PaceZone_RpeMin",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "PaceZone_Zone",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "ReadinessScore",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "RunMinutes",
                table: "ScheduledWorkouts");

            migrationBuilder.DropColumn(
                name: "WalkMinutes",
                table: "ScheduledWorkouts");
        }
    }
}
