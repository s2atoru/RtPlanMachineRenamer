using EvilDICOM.Core;
using EvilDICOM.Core.Element;
using EvilDICOM.Core.Helpers;
using Microsoft.Win32;
using MvvmCommon.ViewModels;
using Prism.Commands;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;

namespace RtPlanMachineRenamer.ViewModels
{
    public class RtPlanViewModel : BindableBaseWithErrorsContainer
    {
        private string rtPlanFilePath;
        [Required(ErrorMessage = "Choose RTPlan file")]
        public string RtPlanFilePath
        {
            get { return rtPlanFilePath; }
            set
            {
                if (SetProperty(ref rtPlanFilePath, value) && !string.IsNullOrEmpty(rtPlanFilePath))
                {
                    OpenRtPlan();
                }

                if (!this.HasErrors)
                {
                    CanOk = true;
                }
                else
                {
                    CanOk = false;
                }
            }
        }

        private string originalMachineName;
        public string OriginalMachineName
        {
            get { return originalMachineName; }
            set { SetProperty(ref originalMachineName, value); }
        }

        private string newMachineName;
        [Required(ErrorMessage = "Enter new machine name")]
        public string NewMachineName
        {
            get { return newMachineName; }
            set
            {
                SetProperty(ref newMachineName, value);
                if (!this.HasErrors)
                {
                    CanOk = true;
                }
                else
                {
                    CanOk = false;
                }
            }
        }

        private string patientId;
        public string PatientId
        {
            get { return patientId; }
            set { SetProperty(ref patientId, value); }
        }

        private string patientName;
        public string PatientName
        {
            get { return patientName; }
            set { SetProperty(ref patientName, value); }
        }

        private string planId;
        public string PlanId
        {
            get { return planId; }
            set { SetProperty(ref planId, value); }
        }

        private bool doesOverwrite = true;
        public bool DoesOverwrite
        {
            get { return doesOverwrite; }
            set { SetProperty(ref doesOverwrite, value); }
        }

        public DelegateCommand ChooseFileCommand { get; }
        public DelegateCommand OkCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public RtPlanViewModel()
        {
            RtPlanFilePath = string.Empty;
            OriginalMachineName = string.Empty;
            NewMachineName = "TrueBeamSN2690";

            ChooseFileCommand = new DelegateCommand(ChooseFile);
            OkCommand = new DelegateCommand(RenameMachine).ObservesCanExecute(() => CanOk);
        }

        private bool canOk;
        public bool CanOk
        {
            get { return canOk; }
            set { SetProperty(ref canOk, value); }
        }

        private DICOMObject RtPlan;

        private void ChooseFile()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "ファイルを開く";
            dialog.Filter = "DICOMファイル(*.dcm)|*.dcm";
            if (dialog.ShowDialog() == true)
            {
                RtPlanFilePath = dialog.FileName;
            }
            else
            {
                RtPlanFilePath = string.Empty;
            }
        }

        private void OpenRtPlan()
        {
            RtPlan = DICOMObject.Read(RtPlanFilePath);

            var modality = (RtPlan.FindFirst(TagHelper.Modality) as CodeString).Data;
            if (modality != "RTPLAN")
            {
                MessageBox.Show($"Modality ({modality}) is not RTPLAN. Choose RTPLAN.");
                PatientName = string.Empty;
                PatientId = string.Empty;
                OriginalMachineName = string.Empty;
                return;
            }
            var strongName = RtPlan.FindFirst(TagHelper.PatientName) as PersonName;
            var firstName = strongName.FirstName;
            var lastName = strongName.LastName;

            PatientName = lastName.ToUpper() + ", " + firstName;

            PatientId = (RtPlan.FindFirst(TagHelper.PatientID) as LongString).Data;

            PlanId = (RtPlan.FindFirst(TagHelper.RTPlanLabel) as ShortString).Data;

            var beamSequence = RtPlan.FindFirst(TagHelper.BeamSequence);
            OriginalMachineName = (((DICOMObject)beamSequence.DData_[0]).FindFirst(TagHelper.TreatmentMachineName) as ShortString).Data;
        }

        private void RenameMachine()
        {
            var beamSequence = RtPlan.FindFirst(TagHelper.BeamSequence);
            var newTreatmentMachineName = new ShortString(TagHelper.TreatmentMachineName, NewMachineName);
            foreach (DICOMObject beam in beamSequence.DData_)
            {
                beam.Replace(newTreatmentMachineName);
            }

            var folderPath = Path.GetDirectoryName(RtPlanFilePath);
            var fileNameRoot = Path.GetFileNameWithoutExtension(RtPlanFilePath);
            var extension = Path.GetExtension(RtPlanFilePath);

            var newFileName = fileNameRoot + "_" + NewMachineName + extension;
            var newFilePath = Path.Combine(folderPath, newFileName);

            if (DoesOverwrite)
            {
                newFilePath = RtPlanFilePath;
            }

            RtPlan.Write(newFilePath);
        }
    }
}
