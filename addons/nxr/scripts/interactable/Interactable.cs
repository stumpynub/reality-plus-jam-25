using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Godot;
using NXR;

namespace NXRInteractable
{
    /// <summary>
    /// Represents an interactable object in the VR environment, supporting grabbing, dropping, and distance interactions.
    /// </summary>
    [GlobalClass]
    public partial class Interactable : RigidBody3D, IHoverable
    {


        public struct InitialState(Node3D parent, Transform3D transform, bool freezemode)
        {
            public Node3D Parent = parent;
            public Transform3D Transform = transform;
            public bool FreezeMode = freezemode;
        }


        public struct GrabInfo(Interactor interactor, Transform3D startGlobalTransform, Transform3D relativeTransform)
        {
            public Interactor Interactor = interactor;
            public Transform3D StartGlobalTransform = startGlobalTransform;
            public Transform3D RelativeTransform = relativeTransform;
        }


        #region Exported Properties
        [Export] public bool Disabled { get; set; } = false;
        [Export(PropertyHint.Range, "0.1, 100")] public float Priority { get; set; } = 1;
        [Export] public HoldMode HoldMode { get; set; } = HoldMode.Hold;


        [ExportGroup("Grab Settings")]
        [Export] public bool FreezeOnGrab = true;
        [Export] public bool SecondaryGrabEnabled = true;
        [Export] public bool DistanceGrabEnabled { get; set; } = false;
        [Export] public float DistanceGrabReach { get; set; } = 4;
        [Export] public float GrabBreakDistance { get; set; } = 0.5f;


        [ExportGroup("Action Settings")]
        [Export] public string GrabAction { get; set; } = "grip_click";
        [Export] public string DropAction { get; set; } = "grip_click";


        [ExportGroup("Drop Settings")]
        [Export] public bool PrimaryFullDrop = false;
        [Export] public bool ResetFreeze = true;
        [Export] private bool _switchOnDrop = false;



        [ExportGroup("Haptic Settings")]
        [Export] private ResHaptic _grabPulse = new(0.5f, 0.5f, 0.1f);
        [Export] private ResHaptic _dropPulse = new(0.5f, 0.5f, 0.1f);



        [ExportGroup("Offsets")]
        public Vector3 PositionOffset { get; set; } = new Vector3();
        public Vector3 RotationOffset { get; set; } = new Vector3();

        #endregion

        #region Public Properties
        public InitialState InitState;
        public bool IsHovered { get; set; }
        public Interactor HoveredInteractor { get; set; }
        public Node3D PreviousParent { get; set; }


        public GrabInfo PrimaryGrab = new();
        public GrabInfo SecondaryGrab = new();


        public Interactable PrimaryGrabPoint { get; set; }
        public Interactable SecondaryGrabPoint { get; set; }
        public Transform3D PrimaryGrabPointOffset { get; set; } = new();
        public Transform3D SecondaryGrabPointOffset { get; set; } = new();
        public HashSet<Interactor> HoveredInteractors { get; set; } = new();

        #endregion

        #region Private Fields

        private Transform3D _secondaryRelativeTransform = new Transform3D();
        private Transform3D _primaryRelativeTransform = new Transform3D();

        #endregion

        #region Signals

        [Signal] public delegate void OnHoveredOutEventHandler(Interactor interactor);
        [Signal] public delegate void OnHoveredEventHandler(Interactor interactor);
        [Signal] public delegate void OnGrabbedEventHandler(Interactable interactable, Interactor interactor);
        [Signal] public delegate void OnDroppedEventHandler(Interactable interactable, Interactor interactor);
        [Signal] public delegate void OnFullDroppedEventHandler();
        [Signal] public delegate void StateUpdatedEventHandler(PhysicsDirectBodyState3D state3D);

        #endregion



        #region Godot Lifecycle Methods

        public override void _Ready()
        {
            Initialize();
        }

        #endregion

        #region Interaction Methods

        public void Grab(Interactor interactor, bool asSecondary = false)
        {
            if (Disabled || interactor == null) return;
            if (PrimaryGrab.Interactor != null && SecondaryGrab.Interactor != null) return;
            

            if (interactor is XRControllerInteractor _interactor)
            {
                _interactor.Controller?.Pulse(_grabPulse.Frequency, _grabPulse.Amplitude, _grabPulse.Duration); 
            }
            
            if (!IsInstanceValid(PrimaryGrab.Interactor) && !asSecondary)
            {
                PrimaryGrab = new GrabInfo(interactor, interactor.GlobalTransform, GlobalTransform * interactor.GlobalTransform.AffineInverse());
                _primaryRelativeTransform = interactor.GlobalTransform.AffineInverse() * GlobalTransform;
                Freeze = FreezeOnGrab;
            }
            else
            {
                SecondaryGrab = new GrabInfo(interactor, interactor.GlobalTransform, GlobalTransform * interactor.GlobalTransform.AffineInverse());
                _secondaryRelativeTransform = SecondaryGrab.Interactor.GlobalTransform.AffineInverse() * GlobalTransform;
            }

            EmitSignal(nameof(OnGrabbed), this, interactor);
        }


        public void Drop(Interactor interactor)
        {

            EmitSignal(nameof(OnDropped), this, interactor);

            Interactor droppedInteractor = interactor;

            if (interactor is XRControllerInteractor _interactor)
            {
                _interactor.Controller?.Pulse(_dropPulse.Frequency, _dropPulse.Amplitude, _dropPulse.Duration); 
            }

            if (interactor == PrimaryGrab.Interactor)
                {
                    PrimaryGrab.Interactor = null;

                    if (PrimaryFullDrop) FullDrop();
                }

            if (interactor == SecondaryGrab.Interactor)
            {
                SecondaryGrab.Interactor = null;
            }


            if (!IsGrabbed())
            {
                Freeze = ResetFreeze == true ? InitState.FreezeMode : Freeze;
                EmitSignal(nameof(OnFullDropped));
            }


            // do after emitting signals to give time for any cleanup 
            if (IsInstanceValid(SecondaryGrab.Interactor))
            {
                _secondaryRelativeTransform = SecondaryGrab.Interactor.GlobalTransform.AffineInverse() * GlobalTransform;

                if (_switchOnDrop && interactor != SecondaryGrab.Interactor)
                {
                    Interactor newPrimary = SecondaryGrab.Interactor;
                    SecondaryGrab.Interactor.Drop();

                    newPrimary.Grab(this);
                }
            }
        }


        public void FullDrop()
        {
            PrimaryGrab.Interactor?.Drop();
            SecondaryGrab.Interactor?.Drop();
        }

        #endregion

        #region Utility Methods
        protected void Initialize()
        {
            //CollisionLayer = (uint)ProjectSettings.GetSetting("NXR/default_interactable_layer", 0);

            InitState = new()
            {
                Parent = GetParent<Node3D>(),
                Transform = Transform,
                FreezeMode = Freeze
            };

            PreviousParent = InitState.Parent;
            PrimaryGrabPoint ??= this;
            SecondaryGrabPoint ??= this;
        }

        public bool IsGrabbed() => IsInstanceValid(PrimaryGrab.Interactor) || IsInstanceValid(SecondaryGrab.Interactor);

        public bool IsTwoHanded() => IsInstanceValid(PrimaryGrab.Interactor) && IsInstanceValid(SecondaryGrab.Interactor);

        public Transform3D GetPrimaryRelativeXform() => PrimaryGrab.Interactor.GlobalTransform * _primaryRelativeTransform;

        public Transform3D GetSecondaryRelativeXform() => SecondaryGrab.Interactor.GlobalTransform * _secondaryRelativeTransform;

        public Controller GetPrimaryController()
        {
            if (PrimaryGrab.Interactor is XRControllerInteractor interactor)
            {
                return interactor.Controller;
            }

            return null;
        }


        public Controller GetSecondaryController()
        {
            if (SecondaryGrab.Interactor is XRControllerInteractor interactor)
            {
                return interactor.Controller;
            }
            return null;
        }

        public Transform3D GetOffsetXform()
        {
            Vector3 rotOffset = RotationOffset * (Vector3.One * (Mathf.Pi / 180));
            Transform3D offsetTransform = GlobalTransform * GlobalTransform.AffineInverse();
            offsetTransform = offsetTransform.TranslatedLocal(PositionOffset);
            offsetTransform.Basis *= Basis.FromEuler(rotOffset);
            return offsetTransform.Orthonormalized();
        }

        public bool IsInteractable() => true;

        #endregion
    }
}