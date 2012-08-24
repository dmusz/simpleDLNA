﻿using System;
using System.IO;
using System.Runtime.Serialization;
using NMaier.sdlna.FileMediaServer.Folders;
using NMaier.sdlna.Server;
using NMaier.sdlna.Server.Metadata;

namespace NMaier.sdlna.FileMediaServer.Files
{
  [Serializable]
  internal class ImageFile : BaseFile, IMetaImageItem, ISerializable
  {

    private Cover _cover = null, cover = null;
    private string creator;
    private string description;
    private uint? width, height;
    private bool initialized = false;
    private string title;



    internal ImageFile(BaseFolder aParent, FileInfo aFile, DlnaTypes aType) : base(aParent, aFile, aType, MediaTypes.IMAGE) { }

    protected ImageFile(SerializationInfo info, StreamingContext ctx)
      : this(null, (ctx.Context as DeserializeInfo).Info, (ctx.Context as DeserializeInfo).Type)
    {
      creator = info.GetString("cr");
      description = info.GetString("d");
      title = info.GetString("t");
      width = info.GetUInt32("w");
      height = info.GetUInt32("h");
      try {
        _cover = cover = info.GetValue("c", typeof(Cover)) as Cover;
      }
      catch (SerializationException) { }

      initialized = true;
    }



    public override IMediaCoverResource Cover
    {
      get
      {
        MaybeInit();
        if (_cover == null) {
          try {
            _cover = base.Cover as Cover;
            _cover.OnCoverLazyLoaded += CoverLoaded;
          }
          catch (Exception) { }
        }
        return _cover;
      }
    }

    public string MetaCreator
    {
      get
      {
        MaybeInit();
        return creator;
      }
    }

    public string MetaDescription
    {
      get
      {
        MaybeInit();
        return description;
      }
    }

    public uint? MetaHeight
    {
      get
      {
        MaybeInit();
        return height;
      }
    }

    public uint? MetaWidth
    {
      get
      {
        MaybeInit();
        return width;
      }
    }

    public override IHeaders Properties
    {
      get
      {
        MaybeInit();
        var rv = base.Properties;
        if (description != null) {
          rv.Add("Description", description);
        }
        if (creator != null) {
          rv.Add("Creator", creator);
        }
        if (width != null && height != null) {
          rv.Add("Resolution", string.Format("{0}x{1}", width.Value, height.Value));
        }
        return rv;
      }
    }

    public override string Title
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(title)) {
          return string.Format("{0} — {1}", base.Title, title);
        }
        return base.Title;
      }
    }




    public void GetObjectData(SerializationInfo info, StreamingContext ctx)
    {
      info.AddValue("cr", creator);
      info.AddValue("d", description);
      info.AddValue("t", title);
      info.AddValue("w", width);
      info.AddValue("h", height);
      info.AddValue("c", cover);
    }

    private void CoverLoaded(object sender, EventArgs e)
    {
      cover = _cover;
      Parent.Server.UpdateFileCache(this);
    }

    private void MaybeInit()
    {
      if (initialized) {
        return;
      }

      try {
        using (var tl = TagLib.File.Create(Item.FullName)) {
          try {
            width = (uint)tl.Properties.PhotoWidth;
            height = (uint)tl.Properties.PhotoHeight;
          }
          catch (Exception ex) {
            Debug("Failed to transpose Properties props", ex);
          }

          try {
            var t = (tl as TagLib.Image.File).ImageTag;
            if (string.IsNullOrWhiteSpace(title = t.Title)) {
              title = null;
            }
            if (string.IsNullOrWhiteSpace(description = t.Comment)) {
              description = null;
            }
            if (string.IsNullOrWhiteSpace(creator = t.Creator)) {
              creator = null;
            }
          }
          catch (Exception ex) {
            Debug("Failed to transpose Tag props", ex);
          }
        }
      }
      catch (TagLib.CorruptFileException ex) {
        Debug("Failed to read metadata via taglib for file " + Item.FullName, ex);
      }
      catch (Exception ex) {
        Warn("Unhandled exception reading metadata for file " + Item.FullName, ex);
      }


      initialized = true;
    }
  }
}