import { SCREEN_MANIFEST } from "./routeManifest";

/** Serializable hand-off for Figma/mobile planning. Route factories stay in the
 * runtime manifest; this projection contains only screen metadata. */
export const FIGMA_SCREEN_MANIFEST = SCREEN_MANIFEST.map((screen) => ({
  screenId: screen.id,
  canonicalPath: screen.canonicalPath,
  pathPatterns: screen.patterns,
  aliases: screen.patterns.slice(1),
  title: screen.title,
  productArea: screen.productArea,
  auth: screen.auth,
  roles: screen.roleAccess,
  shell: screen.shell,
  screenType: screen.screenType,
  navigation: screen.navigation ?? null,
  mobileNavigation: screen.mobileNavigation ?? null,
  figmaRef: screen.figmaRef ?? null,
}));

export type FigmaScreenManifestEntry = (typeof FIGMA_SCREEN_MANIFEST)[number];
