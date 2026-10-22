namespace Cadroue.UIDeportment;

public readonly record struct QExportFace(
    string QExportFaceName,
    bool QExportFaceNative,
    bool QExportFaceSelected,
    bool QExportFaceModified,
    bool QExportFaceSupported,
    bool QExportFaceEditing,
    string QExportFaceNotice);
