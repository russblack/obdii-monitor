﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ObdiiMonitor
{
    class Controller
    {
        private MainWindow mainWindow;

        public MainWindow MainWindow
        {
            get { return mainWindow; }
            set { mainWindow = value; }
        }

        private SensorController sensorController = new SensorController();

        public SensorController SensorController
        {
            get { return sensorController; }
        }

        private LoadController loadController = new LoadController();

        internal LoadController LoadController
        {
            get { return loadController; }
        }

        private SaveController saveController = new SaveController();

        internal SaveController SaveController
        {
            get { return saveController; }
        }

        private Serial serial = new Serial();

        public Serial Serial
        {
            get { return serial; }
        }

        private SensorData sensorData = new SensorData();

        public SensorData SensorData
        {
            get { return sensorData; }
        }

        public Controller()
        {
            sensorController.Controller = this;
            loadController.Controller = this;
            saveController.Controller = this;
            serial.Controller = this;
            sensorData.Controller = this;
        }

        public void cancelAllThreads()
        {
            if (MainWindow.updateGraphPlots != null)
                MainWindow.updateGraphPlots.Abort();

            if (sensorController.polling != null)
                sensorController.polling.Abort();

            if (sensorController.receiving != null)
                sensorController.receiving.Abort();
        }
    }
 }