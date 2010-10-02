﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;

namespace ScanTool
{
    public partial class MainWindow : Form
    {
        public static int START_HEIGHT = 4;
        public static int START_WIDTH = 4;

        Controller controller;

        CheckBox[] checkboxesSensorSelection;

        Label[] labelsSensorSelection;

        Label[] labelsSensorGraphs;

        Label[] labelsSensorGraphsValues;

        Chart[] chartsSensorGraphs;

        Thread updateGraphPlots;

        delegate void SetResponseCallback(int i, PollResponse response);

        Queue graphQueue = new Queue();

        public Queue GraphQueue
        {
            get { return graphQueue; }
        }

        public MainWindow()
        {
            controller = new Controller();
            controller.MainWindow = this;
            InitializeComponent();
            populateSelectionWindow();
            panelSensorGraphs.Visible = false;
            comboBoxBaudRate.SelectedIndex = 1;
            comboBoxComPort.SelectedIndex = 3;
        }


        private void populateSelectionWindow()
        {
            this.panelSensorSelection.Controls.Clear();

            labelsSensorSelection = new Label[controller.SensorController.Sensors.Length];
            checkboxesSensorSelection = new CheckBox[controller.SensorController.Sensors.Length];

            int height = START_HEIGHT;
            int width = START_WIDTH;

            for (int i=0; i < controller.SensorController.Sensors.Length; ++i) 
            {
                labelsSensorSelection[i] = new Label(); 
                labelsSensorSelection[i].Text = controller.SensorController.Sensors[i].Label;
                labelsSensorSelection[i].Location = new Point(width + 20, height + 25 * i + 5);
                this.panelSensorSelection.Controls.Add(labelsSensorSelection[i]);

                checkboxesSensorSelection[i] = new CheckBox();
                checkboxesSensorSelection[i].Location = new Point(width, height + 25 * i);
                checkboxesSensorSelection[i].Checked = true;
                this.panelSensorSelection.Controls.Add(checkboxesSensorSelection[i]);
            }
        }

        private void populateGraphWindow(ArrayList numsSelected)
        {
            this.panelSensorGraphs.Controls.Clear();

            labelsSensorGraphs = new Label[numsSelected.Count];
            labelsSensorGraphsValues = new Label[controller.SensorController.SelectedSensors.Length];
            chartsSensorGraphs = new Chart[numsSelected.Count];

            ChartArea[] chartAreas = new ChartArea[numsSelected.Count];
            Legend[] legends = new Legend[numsSelected.Count];
            Series[] seriesLines = new Series[numsSelected.Count];
            Series[] seriesPoints = new Series[numsSelected.Count];

            int height = START_HEIGHT;
            int width = START_WIDTH;

            for (int i = 0; i < numsSelected.Count; ++i)
            {
                labelsSensorGraphs[i] = new Label();
                labelsSensorGraphs[i].Text = controller.SensorController.Sensors[(int)numsSelected[i]].Label;
                labelsSensorGraphs[i].Location = new Point(width, height + 200 * i);
                this.panelSensorGraphs.Controls.Add(labelsSensorGraphs[i]);

                labelsSensorGraphsValues[i] = new Label();
                labelsSensorGraphsValues[i].Text = "Value: ";
                labelsSensorGraphsValues[i].Location = new Point(width + labelsSensorGraphs[i].Size.Width + 5, height + 200 * i);
                this.panelSensorGraphs.Controls.Add(labelsSensorGraphsValues[i]);

                chartsSensorGraphs[i] = new Chart();
                chartAreas[i] = new ChartArea();
                chartAreas[i].AlignmentStyle = AreaAlignmentStyles.All;
                chartAreas[i].AxisX.IsReversed = true;
                legends[i] = new Legend();
                seriesLines[i] = new Series();

                seriesPoints[i] = new Series();

                chartsSensorGraphs[i].ChartAreas.Add(chartAreas[i]);
                chartsSensorGraphs[i].Legends.Add(legends[i]);
                seriesLines[i].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                seriesPoints[i].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                chartsSensorGraphs[i].Series.Add(seriesLines[i]);
                chartsSensorGraphs[i].Series.Add(seriesPoints[i]);
                chartsSensorGraphs[i].Location = new System.Drawing.Point(23, 50);
                chartsSensorGraphs[i].Size = new System.Drawing.Size(170, 170);
                chartsSensorGraphs[i].Location = new Point(width, height + 200 * i + 25);
                this.panelSensorGraphs.Controls.Add(chartsSensorGraphs[i]);
            }
        }

        private void buttonCollect_Click(object sender, EventArgs e)
        {
            if (buttonCollect.Text == "Collect Data")
            {
                ArrayList numsSelected = new ArrayList();
                for (int i = 0; i < checkboxesSensorSelection.Length; ++i)
                    if (checkboxesSensorSelection[i].Checked)
                        numsSelected.Add(i);

                controller.SensorController.initializeSelectedSensors(numsSelected);
                controller.SensorController.initializePollingReceivingThreads();
                populateGraphWindow(numsSelected);
                buttonCollect.Text = "Stop";
                this.panelSensorSelection.Visible = false;
                this.panelSensorGraphs.Visible = true;
                updateGraphPlots = new Thread(new ThreadStart(updateGraphs));
                updateGraphPlots.Name = "UpdateGraphs";
                updateGraphPlots.Start();
            }
            else if (buttonCollect.Text == "Stop")
            {
                populateSelectionWindow();
                buttonCollect.Text = "Collect Data";
                controller.SensorController.stopPollingReceiving();
                this.panelSensorSelection.Visible = true;
                this.panelSensorGraphs.Visible = false;
                updateGraphPlots.Abort();
            }
        }

        private void updateGraphs()
        {
            while (true)
            {
                if (graphQueue.Count != 0)
                {
                    PollResponse response = (PollResponse)graphQueue.Dequeue();
                    
                    for (int i = 0; i < controller.SensorController.SelectedSensors.Length; ++i)
                    {
                        if (response.DataTag == controller.SensorController.SelectedSensors[i].Pid)
                        {
                            setGraphPoint(i, response);
                            break;
                        }
                    }
                }
            }
        }

        private void setGraphPoint(int i, PollResponse response)
        {

			if (this.chartsSensorGraphs[i].InvokeRequired)
			{	
				SetResponseCallback d = new SetResponseCallback(setGraphPoint);
				this.Invoke(d, new object[] {i,  response });
			}
			else
			{
                foreach (Series series in chartsSensorGraphs[i].Series)
                {
                    series.Points.Add(new DataPoint((double)response.Time.TimeOfDay.TotalSeconds, response.convertData()));
                }
                chartsSensorGraphs[i].Size = new System.Drawing.Size(chartsSensorGraphs[i].Size.Width + 40, chartsSensorGraphs[i].Size.Height);
                labelsSensorGraphsValues[i].Text = "Value: " + response.convertData();
			}
		}

        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            try
            {
                controller.Serial.initialize(comboBoxBaudRate.Text, comboBoxComPort.Text);
                labelStatus.Text = comboBoxComPort.Text + " now open.";
            }
            catch (Exception ex)
            {
                labelStatus.Text = "Could not connect. Try again.";
                MessageBox.Show(ex.Message);
            }
        }
    }
}