using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseWall : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("1169CEB6-381C-4353-8ACE-874938755694");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "AW";

    public AnalyseWall() : base(
      name: "Analyse Wall",
      nickname: "A-W",
      description: "Analyse given Wall element",
      category: "Revit",
      subCategory: "Analyse"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Wall",
        nickname: "W",
        description: "Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.WallSystemFamily_ValueList(),
        name: "Wall System Family",
        nickname: "WSF",
        description: "System family (DB.WallKind) of the given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Wall Type",
        nickname: "WT",
        description: "Element type (DB.WallType) of the given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Parent Stacked Wall",
        nickname: "PSW",
        description: "Parent Stacked Wall instance if given wall is a member of a Stacked Wall",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Level(),
        name: "Base Level",
        nickname: "BL",
        description: "Base level (constraint) of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Base Level Offset",
        nickname: "BLO",
        description: "Base level offset of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Bottom Is Attached",
        nickname: "BLA?",
        description: "Whether the wall instance is attached to the base level",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Level(),
        name: "Top Level",
        nickname: "TL",
        description: "Top level (constraint) of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Top Level Offset",
        nickname: "TLO",
        description: "Top level offset of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Top Is Attached",
        nickname: "TLA?",
        description: "Whether the wall instance is attached to the top level",
        access: GH_ParamAccess.item
        );

      manager.AddNumberParameter(
        name: "Height",
        nickname: "H",
        description: "Height of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Length",
        nickname: "L",
        description: "Length of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Width",
        nickname: "W",
        description: "Width of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Area",
        nickname: "A",
        description: "Area of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Volume",
        nickname: "V",
        description: "Volume of given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddBooleanParameter(
        name: "Is Room Bounding",
        nickname: "RB?",
        description: "Whether given wall instance is room bounding",
        access: GH_ParamAccess.item
        );


      manager.AddBooleanParameter (
        name: "Structural",
        nickname: "ST?",
        description: "Whether given wall instance is structural",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.WallStructuralUsage_ValueList(),
        name: "Structural Usage",
        nickname: "STU",
        description: "Structural usage of given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddVectorParameter(
        name: "Orientation",
        nickname: "O",
        description: "Orientation vector of given wall instance",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Wall wallInstance = default;
      if (!DA.GetData("Wall", ref wallInstance))
        return;

      DA.SetData("Wall System Family", new Types.WallSystemFamily(wallInstance.WallType.Kind));
      DA.SetData("Wall Type", Types.ElementType.FromElement(wallInstance.WallType));
      if (wallInstance.IsStackedWallMember)
        DA.SetData("Parent Stacked Wall", Types.Element.FromElement(wallInstance.Document.GetElement(wallInstance.StackedWallOwnerId)));

      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_BASE_CONSTRAINT, "Base Level");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_BASE_OFFSET, "Base Level Offset");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_BOTTOM_IS_ATTACHED, "Bottom Is Attached");

      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_HEIGHT_TYPE, "Top Level");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_TOP_OFFSET, "Top Level Offset");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_TOP_IS_ATTACHED, "Top Is Attached");

      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM, "Height");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.CURVE_ELEM_LENGTH, "Length");
      DA.SetData("Width", wallInstance.Width);
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.HOST_AREA_COMPUTED, "Area");
      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.HOST_VOLUME_COMPUTED, "Volume");

      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_ATTR_ROOM_BOUNDING, "Is Room Bounding");

      PipeHostParameter(DA, wallInstance, DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, "Structural");
      PipeHostParameter<Types.StructuralWallUsage>(DA, wallInstance, DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM, "Structural Usage");

      var wallOrientationVector = new Rhino.Geometry.Vector3d(wallInstance.Orientation.X, wallInstance.Orientation.Y, wallInstance.Orientation.Z);
      if (wallInstance.Flipped)
        wallOrientationVector.Reverse();
      DA.SetData("Orientation", wallOrientationVector);
    }
  }
}