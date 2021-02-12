﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Settings {

        public static void Edit(Marana.Settings settings) {
            Window window = Utility.SetWindow("Edit Settings");

            // Layout coordinates

            int x1 = 1;
            int x2 = 30, w2 = 50;

            // Item labels

            window.Add(
                new Label("Config Directory") { X = x1, Y = 1 },
                new Label(Marana.Settings.GetConfigDirectory()) { X = x2, Y = 1 },

                new Label("Working Directory") { X = x1, Y = 2 },

                new Label("Alpaca API Key") { X = x1, Y = 4 },
                new Label("Alpaca API Secret") { X = x1, Y = 5 },

                new Label("Database Server") { X = x1, Y = 7 },
                new Label("Database Port") { X = x1, Y = 8 },
                new Label("Database Schema") { X = x1, Y = 9 },
                new Label("Database Username") { X = x1, Y = 10 },
                new Label("Database Password") { X = x1, Y = 11 }
            );

            // Fields for text entry

            TextField tfWorkingDir = new TextField(settings.Directory_Working) {
                X = x2, Y = 2, Width = w2
            };

            TextField tfAlpacaKey = new TextField(settings.API_Alpaca_Key) {
                X = x2, Y = 4, Width = w2
            };
            TextField tfAlpacaSecret = new TextField(settings.API_Alpaca_Secret) {
                X = x2, Y = 5, Width = w2
            };

            TextField tfDbServer = new TextField(settings.Database_Server) {
                X = x2, Y = 7, Width = w2
            };
            TextField tfDbPort = new TextField(settings.Database_Port.ToString()) {
                X = x2, Y = 8, Width = w2
            };
            TextField tfDbSchema = new TextField(settings.Database_Schema) {
                X = x2, Y = 9, Width = w2
            };
            TextField tfDbUsername = new TextField(settings.Database_Username) {
                X = x2, Y = 10, Width = w2
            };
            TextField tfDbPassword = new TextField(settings.Database_Password) {
                X = x2, Y = 11, Width = w2
            };

            window.Add(
                tfWorkingDir,
                tfAlpacaKey,
                tfAlpacaSecret,
                tfDbServer,
                tfDbPort,
                tfDbSchema,
                tfDbUsername,
                tfDbPassword
                );

            // Dialog notification on save success

            Dialog dlgSaved = Utility.DialogNotification_Okay("Settings saved successfully.", 40, 7, window);

            // Button for saving settings

            Button btnSave = new Button("Save Settings") {
                X = Pos.Center(), Y = 14
            };

            btnSave.Clicked += () => {
                int portResult;
                bool portParse = int.TryParse(tfDbPort.Text.ToString(), out portResult);
                Marana.Settings newSettings = new Marana.Settings() {
                    Directory_Working = tfWorkingDir.Text.ToString().Trim(),
                    API_Alpaca_Key = tfAlpacaKey.Text.ToString().Trim(),
                    API_Alpaca_Secret = tfAlpacaSecret.Text.ToString().Trim(),
                    Database_Server = tfDbServer.Text.ToString().Trim(),
                    Database_Schema = tfDbSchema.Text.ToString().Trim(),
                    Database_Username = tfDbUsername.Text.ToString().Trim(),
                    Database_Password = tfDbPassword.Text.ToString().Trim(),
                };

                newSettings.Database_Port = portParse ? portResult : newSettings.Database_Port;

                Marana.Settings.SaveConfig(newSettings);

                window.Add(dlgSaved);
            };

            window.Add(btnSave);

            Application.Run();
        }
    }
}