Name:           lizard-keyboard-sounds
Version:        %{version}
Release:        %{release}%{?dist}
Summary:        Per-key keyboard sound desktop application
License:        LicenseRef-Proprietary
BuildArch:      x86_64
Requires:       libX11
Requires:       libXi
Requires:       libXtst

%description
Lizard Keyboard Sounds assigns audio to keyboard keys and runs in the desktop tray.

%prep

%build

%install
mkdir -p %{buildroot}/usr/lib/lizard-keyboard-sounds
cp -a %{source_dir}/. %{buildroot}/usr/lib/lizard-keyboard-sounds/
install -D -m 0644 %{_sourcedir}/lizard-keyboard-sounds.desktop \
  %{buildroot}/usr/share/applications/lizard-keyboard-sounds.desktop
install -D -m 0644 %{_sourcedir}/lizard-keyboard-sounds.png \
  %{buildroot}/usr/share/icons/hicolor/256x256/apps/lizard-keyboard-sounds.png

%files
/usr/lib/lizard-keyboard-sounds
/usr/share/applications/lizard-keyboard-sounds.desktop
/usr/share/icons/hicolor/256x256/apps/lizard-keyboard-sounds.png
