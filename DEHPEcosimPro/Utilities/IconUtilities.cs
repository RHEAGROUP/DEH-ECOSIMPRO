// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IconUtilities.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
//
//    This file is part of DEHPEcosimPro
//
//    The DEHPEcosimPro is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
//
//    The DEHPEcosimPro is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPEcosimPro.Utilities
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using CDP4Common.CommonData;

    using DEHPEcosimPro.Services.IconCacheService;

    using DevExpress.Xpf.Core;
    using DevExpress.Xpf.Core.Native;

    using Point = System.Drawing.Point;

    /// <summary>
    /// Utility class containing static method to handle icons
    /// </summary>
    public static class IconUtilities
    {
        /// <summary>
        /// The root path for resources
        /// </summary>
        public const string RootResourcesPath = "pack://application:,,,/";

        /// <summary>
        /// The <see cref="Uri"/> to the error image overlay.
        /// </summary>
        public static readonly Uri ErrorImageUri = new Uri($"{RootResourcesPath}Resources/Images/ExclamationRed_16x16.png");

        /// <summary>
        /// The <see cref="Uri"/> to the relationship image overlay.
        /// </summary>
        public static readonly Uri RelationshipOverlayUri = new Uri($"{RootResourcesPath}Resources/Images/linkgreen_16x16.png");

        /// <summary>
        /// The <see cref="Uri"/> to the favorite image overlay.
        /// </summary>
        public static readonly Uri FavoriteOverlayUri = (new DXImageConverter().ConvertFrom("NewContact_16x16.png") as DXImageInfo)?.MakeUri();

        /// <summary>
        /// The <see cref="Uri"/> to the locked image overlay.
        /// </summary>
        public static readonly Uri LockedOverlayUri = (new DXImageConverter().ConvertFrom("BO_Security_Permission.png") as DXImageInfo)?.MakeUri();

        /// <summary>
        /// The <see cref="Uri"/> to the hidden image overlay.
        /// </summary>
        public static readonly Uri HiddenOverlayUri = new Uri($"{RootResourcesPath}Resources/Images/hidden_16x16.png");

        public static ImageSource ToImageSource(this Icon icon)
        {
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return imageSource;
        }

        /// <summary>
        /// converts the <see cref="BitmapImage"/> to a <see cref="Bitmap"/>
        /// </summary>
        /// <param name="bitmapImage">
        /// the subject <see cref="BitmapImage"/>
        /// </param>
        /// <returns>
        /// the resulting <see cref="Bitmap"/>
        /// </returns>
        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage, null, null, null));
                enc.Save(outStream);

                var bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        /// <summary>
        /// converts the <see cref="BitmapImage"/> to a <see cref="Bitmap"/>
        /// </summary>
        /// <param name="bitmapImage"> the subject <see cref="BitmapImage"/> </param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <returns>the resulting <see cref="Bitmap"/></returns>
        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage, int width, int height)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();

                enc.Frames.Add(BitmapFrame.Create(bitmapImage, null, null, null));
                enc.Save(outStream);

                var bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap, width, height);
            }
        }

        /// <summary>
        /// The bitmap source with the image and the error overlay.
        /// </summary>
        /// <param name="uri">
        /// The uri to the source image.
        /// </param>
        /// <returns>
        /// The <see cref="BitmapSource"/>.
        /// </returns>
        public static BitmapSource WithErrorOverlay(Uri uri)
        {
            return WithOverlay(uri, ErrorImageUri);
        }

        /// <summary>
        /// The bitmap source with the image and the overlay.
        /// </summary>
        /// <param name="iconUri">
        /// The uri to the source image.
        /// </param>
        /// <param name="overlayUri">
        /// The uri of the overlay
        /// </param>
        /// <param name="overlayPosition">
        /// The overlay position
        /// </param>
        /// <returns>
        /// The <see cref="BitmapSource"/>.
        /// </returns>
        public static BitmapSource WithOverlay(Uri iconUri, Uri overlayUri, OverlayPositionKind overlayPosition = OverlayPositionKind.TopLeft)
        {
            var source = new BitmapImage(iconUri);
            var overlay = new BitmapImage(overlayUri);

            var thingBitMapImage = BitmapImage2Bitmap(source);
            var overlayBitMapImage = BitmapImage2Bitmap(overlay, (int)Math.Floor(thingBitMapImage.Width * 0.75D), (int)Math.Floor(thingBitMapImage.Height * 0.75D));

            var image = new Bitmap(
                    (int)Math.Floor(thingBitMapImage.Width * 1.75D),
                    (int)Math.Floor(thingBitMapImage.Height * 1.0D),
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(image))
            {
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(thingBitMapImage, GetMainImagePoint(image, thingBitMapImage));
                graphics.DrawImage(overlayBitMapImage, GetOverlayPoint(overlayPosition, image, overlayBitMapImage));
            }

            return Bitmap2BitmapSource(image);
        }

        /// <summary>
        /// Gets the starting <see cref="Point"/> where the overlay needs to be drawn
        /// </summary>
        /// <param name="overlayPosition">The <see cref="OverlayPositionKind"/></param>
        /// <param name="targetImage">The <see cref="Image"/> that the overlay is added to</param>
        /// <param name="overlayBitMapImage">The overlay<see cref="Image"/></param>
        /// <returns>Starting <see cref="Point"/> where the overlay needs to be drawn</returns>
        private static Point GetOverlayPoint(OverlayPositionKind overlayPosition, Image targetImage, Image overlayBitMapImage)
        {
            var rightXpos = targetImage.Width - overlayBitMapImage.Width;
            var bottomYpos = targetImage.Height - overlayBitMapImage.Height;

            switch (overlayPosition)
            {
                case OverlayPositionKind.BottomLeft:
                    return new Point(0, bottomYpos);

                case OverlayPositionKind.BottomRight:
                    return new Point(rightXpos, bottomYpos);

                case OverlayPositionKind.TopLeft:
                    return new Point(0, 0);

                case OverlayPositionKind.TopRight:
                    return new Point(rightXpos, 0);

                default:
                    return new Point(0, 0);
            }
        }

        /// <summary>
        /// Gets the starting <see cref="Point"/> where the main image needs to be drawn
        /// </summary>
        /// <param name="targetImage">The <see cref="Image"/> that the main image is added to</param>
        /// <param name="thingBitMapImage">The main <see cref="Image"/></param>
        /// <returns>Starting <see cref="Point"/> where the main image needs to be drawn</returns>
        private static Point GetMainImagePoint(Image targetImage, Image thingBitMapImage)
        {
            var rightXpos = (targetImage.Width - thingBitMapImage.Width) / 2;

            return new Point(rightXpos, 0);
        }

        /// <summary>
        /// Converts bitmap to bitmap source.
        /// </summary>
        /// <param name="bitmap">
        /// The bitmap.
        /// </param>
        /// <returns>
        /// The <see cref="BitmapImage"/>.
        /// </returns>
        public static BitmapSource Bitmap2BitmapSource(Bitmap bitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Returns the <see cref="Uri"/> of the resource
        /// </summary>
        /// <param name="classKind">
        /// The <see cref="ClassKind"/> for which in icon needs to be provided
        /// </param>
        /// <param name="getSmallIcon">
        /// Indicates whether a small or large icon should be returned.
        /// </param>
        /// <returns>
        /// A <see cref="Uri"/> that points to a resource
        /// </returns>
        public static object ImageUri(ClassKind classKind, bool getSmallIcon = true)
        {
            var resourcesPath = $"{RootResourcesPath}Resources/Images/Thing/";
            var imageSize = getSmallIcon ? "_16x16" : "_32x32";
            const string extension = ".png";

            var image = classKind switch
            {
                ClassKind.BinaryRelationship => "LineItem",
                ClassKind.MultiRelationship => "Line2",
                ClassKind.Page => "ListBox",
                ClassKind.Section => "Reading",
                ClassKind.BinaryNote => "Notes",
                ClassKind.TextualNote => "Notes",
                ClassKind.DiagramCanvas => "LabelsRight",
                ClassKind.UnitPrefix => "VerticalAxisThousands",
                ClassKind.RequirementsContainer => "ListBox",
                ClassKind.RequirementsGroup => "ListBox",
                ClassKind.RequirementsSpecification => "BOReport",
                ClassKind.IntervalScale => "ChartYAxisSettings",
                ClassKind.LogarithmicScale => "ChartYAxisSettings",
                ClassKind.OrdinalScale => "ChartYAxisSettings",
                ClassKind.RatioScale => "ChartYAxisSettings",
                ClassKind.CyclicRatioScale => "ChartYAxisSettings",
                ClassKind.MeasurementScale => "ChartYAxisSettings",
                ClassKind.Constant => "RecentlyUse",
                ClassKind.BinaryRelationshipRule => "LineItem",
                ClassKind.Rule => "TreeView",
                ClassKind.DecompositionRule => "TreeView",
                ClassKind.MultiRelationshipRule => "DocumentMap",
                ClassKind.ParameterizedCategoryRule => "FixedWidth",
                ClassKind.ReferencerRule => "Tag",
                ClassKind.SiteDirectory => "Database",
                ClassKind.EngineeringModelSetup => "Technology",
                ClassKind.EngineeringModel => "Technology",
                ClassKind.ParametricConstraint => "ShowFormulas",
                ClassKind.NotExpression => "UseInFormula",
                ClassKind.AndExpression => "UseInFormula",
                ClassKind.OrExpression => "UseInFormula",
                ClassKind.ExclusiveOrExpression => "UseInFormula",
                ClassKind.RelationalExpression => "UseInFormula",
                ClassKind.BooleanExpression => "UseInFormula",
                ClassKind.File => "BOFileAttachment",
                ClassKind.FileRevision => "Version",
                ClassKind.Folder => "BOFolder",
                ClassKind.NotThing => "BOFolder",
                ClassKind.Participant => "Employee",
                ClassKind.Iteration => "GroupFieldCollection",
                ClassKind.IterationSetup => "GroupFieldCollection",
                ClassKind.ElementDefinition => "Product",
                ClassKind.ElementUsage => "Version",
                ClassKind.ParameterGroup => "BOFolder",
                ClassKind.Parameter => "Stepline",
                ClassKind.SimpleParameterValue => "Stepline",
                ClassKind.ParameterValueSet => "DocumentMap",
                ClassKind.ParameterOverrideValueSet => "DocumentMap",
                ClassKind.ParameterSubscriptionValueSet => "DocumentMap",
                ClassKind.ParameterValueSetBase => "DocumentMap",
                ClassKind.ParameterSubscription => "LabelsCenter",
                ClassKind.ParameterOverride => "LabelsBelow",
                ClassKind.PersonRole => "BOUser",
                ClassKind.ParticipantRole => "BOUser",
                ClassKind.Person => "Customer",
                ClassKind.PersonPermission => "BOPermission",
                ClassKind.ParticipantPermission => "BOPermission",
                ClassKind.ReferenceSource => "Information",
                ClassKind.EmailAddress => "Mail",
                ClassKind.TelephoneNumber => "BOContact",
                ClassKind.UserPreference => "Technology",
                ClassKind.SimpleQuantityKind => "NameManager",
                ClassKind.DerivedQuantityKind => "NameManager",
                ClassKind.SpecializedQuantityKind => "NameManager",
                ClassKind.ArrayParameterType => "NameManager",
                ClassKind.BooleanParameterType => "NameManager",
                ClassKind.CompoundParameterType => "NameManager",
                ClassKind.DateParameterType => "NameManager",
                ClassKind.DateTimeParameterType => "NameManager",
                ClassKind.EnumerationParameterType => "NameManager",
                ClassKind.ScalarParameterType => "NameManager",
                ClassKind.TextParameterType => "NameManager",
                ClassKind.TimeOfDayParameterType => "NameManager",
                ClassKind.ParameterTypeComponent => "NameManager",
                ClassKind.ParameterType => "NameManager",
                ClassKind.Definition => "SendBehindText",
                ClassKind.Option => "Properties",
                ClassKind.Term => "TextBox",
                ClassKind.Glossary => "Text",
                ClassKind.FileType => "TextBox2",
                ClassKind.Publication => "CreateModelDifferences",
                ClassKind.CommonFileStore => "Project",
                ClassKind.DomainFileStore => "Project",
                ClassKind.ChangeRequest => "EditComment",
                ClassKind.RequestForDeviation => "EditComment",
                ClassKind.RequestForWaiver => "EditComment",
                ClassKind.ActionItem => "GroupByResource",
                ClassKind.ChangeProposal => "PreviousComment",
                ClassKind.ReviewItemDiscrepancy => "InsertComment",
                ClassKind.ActualFiniteState => $"{resourcesPath}ActualFiniteState_48x48{extension}",
                ClassKind.ActualFiniteStateList => $"{resourcesPath}ActualFiniteState_48x48{extension}",
                ClassKind.PossibleFiniteState => $"{resourcesPath}PossibleFiniteState_48x48{extension}",
                ClassKind.PossibleFiniteStateList => $"{resourcesPath}PossibleFiniteState_48x48{extension}",
                ClassKind.NaturalLanguage => $"{resourcesPath}naturallanguage{extension}",
                ClassKind.Requirement => $"{resourcesPath}requirement{extension}",
                ClassKind.Book => $"{resourcesPath}Book{extension}",
                ClassKind.SimpleUnit => $"{resourcesPath}measurementunit{imageSize}{extension}",
                ClassKind.PrefixedUnit => $"{resourcesPath}measurementunit{imageSize}{extension}",
                ClassKind.LinearConversionUnit => $"{resourcesPath}measurementunit{imageSize}{extension}",
                ClassKind.DerivedUnit => $"{resourcesPath}measurementunit{imageSize}{extension}",
                ClassKind.MeasurementUnit => $"{resourcesPath}measurementunit{imageSize}{extension}",
                ClassKind.DomainOfExpertise => $"{resourcesPath}domainofexpertise{imageSize}{extension}",
                ClassKind.ModelReferenceDataLibrary => $"{resourcesPath}siteRdl{imageSize}{extension}",
                ClassKind.SiteReferenceDataLibrary => $"{resourcesPath}siteRdl{imageSize}{extension}",
                ClassKind.Category => $"{resourcesPath}category{imageSize}{extension}",
                ClassKind.Organization => $"{resourcesPath}Organization{imageSize}{extension}",
                _ => "Technology"
            };

            if (image.Contains(extension))
            {
                return image;
            }

            var imageInfo = (DXImageInfo) new DXImageConverter().ConvertFrom($"{image}{imageSize}{extension}");
            return imageInfo?.MakeUri().ToString();
        }
    }
}
