import type { Config } from '@measured/puck';
import { ButtonBlock } from './components/ButtonBlock';
import { Card } from './components/Card';
import { Flex } from './components/Flex';
import { Grid } from './components/Grid';
import { Heading } from './components/Heading';
import { Hero } from './components/Hero';
import { Logos } from './components/Logos';
import { Space } from './components/Space';
import { Stats } from './components/Stats';
import { Text } from './components/Text';

export const puckConfig: Config = {
  components: {
    Hero,
    Heading,
    Text,
    ButtonBlock,
    Card,
    Grid,
    Flex,
    Stats,
    Logos,
    Space,
  },
};
