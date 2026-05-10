Name:           pizzaoven
Version:        1.0.1
Release:        1%{?dist}
Summary:        Cross-platform mod manager for Pizza Tower
License:        GPLv3
URL:            https://github.com/Cristiandis/PizzaOven-Avalonia
Source0:        https://github.com/Cristiandis/PizzaOven-Avalonia/releases/download/v%{version}/PizzaOven-linux-x64.tar.gz

Requires:       dotnet-runtime-8.0
Requires:       xdelta

BuildArch:      x86_64

%description
Pizza Oven allows you to download, install, and manage mods
for Pizza Tower using GameBanana integration.

%prep
%setup -q -c

%install
install -Dm755 PizzaOven %{buildroot}/usr/lib/pizzaoven/PizzaOven

# Wrapper script
install -dm755 %{buildroot}/usr/bin
cat > %{buildroot}/usr/bin/pizzaoven << 'EOF'
#!/bin/sh
exec /usr/lib/pizzaoven/PizzaOven "$@"
EOF
chmod 755 %{buildroot}/usr/bin/pizzaoven


# Desktop entries
install -dm755 %{buildroot}/usr/share/applications
cat > %{buildroot}/usr/share/applications/pizzaoven.desktop << 'EOF'
[Desktop Entry]
Name=Pizza Oven
Comment=Mod manager for Pizza Tower
Exec=/usr/bin/pizzaoven
Icon=pizzaoven
Type=Application
Categories=Game;
EOF

cat > %{buildroot}/usr/share/applications/pizzaoven-handler.desktop << 'EOF'
[Desktop Entry]
Name=Pizza Oven
Exec=/usr/bin/pizzaoven -download %u
Type=Application
NoDisplay=true
MimeType=x-scheme-handler/pizzaoven;
EOF

# Icon
install -Dm644 Assets/PizzaOvenIcon.png %{buildroot}/usr/share/pixmaps/pizzaoven.png 2>/dev/null || true

%post
xdg-mime default pizzaoven-handler.desktop x-scheme-handler/pizzaoven
update-desktop-database /usr/share/applications || true

%files
/usr/lib/pizzaoven/PizzaOven
/usr/bin/pizzaoven
/usr/share/applications/pizzaoven.desktop
/usr/share/applications/pizzaoven-handler.desktop
/usr/share/icons/hicolor/256x256/apps/pizzaoven.png

%changelog
* Sun May 10 2026 Cristiandis <pizzaoven@izzoserver.top> - 1.0.0-1
- Initial release
