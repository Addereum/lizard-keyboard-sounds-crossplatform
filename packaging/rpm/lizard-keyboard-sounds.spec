Name:           lizard-keyboard-sounds
Version:        %{version}
Release:        %{release}%{?dist}
Summary:        Per-key keyboard sound desktop application
License:        LicenseRef-Proprietary
BuildArch:      x86_64
Requires:       libX11
Requires:       libXi
Requires:       libXtst
Requires(pre):  shadow-utils
Requires(post): systemd-udev
Requires(post): kmod
Requires(postun): shadow-utils
Requires(postun): systemd-udev

%description
Lizard Keyboard Sounds assigns audio to keyboard keys and runs in the desktop tray.
This RPM grants the packaged application access to global input devices through a
dedicated system group and udev rules. The application itself does not run as root.

%prep

%build

%install
mkdir -p %{buildroot}/usr/lib/lizard-keyboard-sounds
cp -a %{source_dir}/. %{buildroot}/usr/lib/lizard-keyboard-sounds/
install -D -m 0644 %{_sourcedir}/lizard-keyboard-sounds.desktop \
  %{buildroot}/usr/share/applications/lizard-keyboard-sounds.desktop
install -D -m 0644 %{_sourcedir}/lizard-keyboard-sounds.png \
  %{buildroot}/usr/share/icons/hicolor/256x256/apps/lizard-keyboard-sounds.png
install -D -m 0644 %{_sourcedir}/lizard-keyboard-sounds.rules \
  %{buildroot}/usr/lib/udev/rules.d/69-lizard-keyboard-sounds.rules

%pre
getent group lizard-keyboard-sounds >/dev/null || \
  /usr/sbin/groupadd --system lizard-keyboard-sounds

%post
/usr/bin/chown root:lizard-keyboard-sounds /usr/lib/lizard-keyboard-sounds/LizardCrossPlatform
/usr/bin/chmod 2755 /usr/lib/lizard-keyboard-sounds/LizardCrossPlatform
/usr/sbin/modprobe uinput >/dev/null 2>&1 || :
/usr/bin/udevadm control --reload-rules >/dev/null 2>&1 || :
/usr/bin/udevadm trigger --subsystem-match=input --subsystem-match=misc >/dev/null 2>&1 || :

%postun
if [ "$1" -eq 0 ]; then
  /usr/sbin/groupdel lizard-keyboard-sounds >/dev/null 2>&1 || :
fi
/usr/bin/udevadm control --reload-rules >/dev/null 2>&1 || :
/usr/bin/udevadm trigger --subsystem-match=input --subsystem-match=misc >/dev/null 2>&1 || :

%files
/usr/lib/lizard-keyboard-sounds
/usr/share/applications/lizard-keyboard-sounds.desktop
/usr/share/icons/hicolor/256x256/apps/lizard-keyboard-sounds.png
/usr/lib/udev/rules.d/69-lizard-keyboard-sounds.rules
